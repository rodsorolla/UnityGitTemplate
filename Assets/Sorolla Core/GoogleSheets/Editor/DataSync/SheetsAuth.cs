using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Service-account OAuth for the Google Sheets API.
    ///
    /// Flow:
    ///  1. Load credentials.json (client_email + private_key).
    ///  2. Build an RS256-signed JWT with the sheets scope.
    ///  3. Exchange at https://oauth2.googleapis.com/token for an access_token.
    ///  4. Cache the token until it nears expiry (refresh 60s early).
    ///
    /// Editor-only; never ships in a build.
    /// </summary>
    public static class SheetsAuth
    {
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string Scope = "https://www.googleapis.com/auth/spreadsheets";
        private const string JwtBearerGrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer";

        private static string _cachedToken;
        private static DateTime _cachedExpiresUtc;
        private static string _cachedCredentialsPath;

        public static void InvalidateCache()
        {
            _cachedToken = null;
            _cachedExpiresUtc = default;
            _cachedCredentialsPath = null;
        }

        /// <summary>
        /// Returns a valid bearer token. Loads/refreshes as needed.
        /// Throws if the credentials file is missing or malformed.
        /// </summary>
        public static async UniTask<string> GetAccessTokenAsync(string credentialsPath)
        {
            if (_cachedToken != null
                && _cachedCredentialsPath == credentialsPath
                && DateTime.UtcNow < _cachedExpiresUtc.AddSeconds(-60))
            {
                return _cachedToken;
            }

            if (!File.Exists(credentialsPath))
                throw new FileNotFoundException($"Service-account credentials not found at: {credentialsPath}");

            var json = File.ReadAllText(credentialsPath);
            var creds = JsonConvert.DeserializeObject<ServiceAccountJson>(json);
            if (creds == null || string.IsNullOrEmpty(creds.ClientEmail) || string.IsNullOrEmpty(creds.PrivateKey))
                throw new InvalidDataException("credentials.json is missing client_email or private_key.");

            var jwt = BuildSignedJwt(creds);

            var body = $"grant_type={UnityWebRequest.EscapeURL(JwtBearerGrantType)}&assertion={UnityWebRequest.EscapeURL(jwt)}";
            using var req = new UnityWebRequest(TokenEndpoint, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            await req.SendWebRequest().ToUniTask();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"Google token exchange failed ({req.responseCode}): {req.downloadHandler.text}");

            var resp = JObject.Parse(req.downloadHandler.text);
            var token = resp.Value<string>("access_token");
            var expiresIn = resp.Value<int?>("expires_in") ?? 3600;
            if (string.IsNullOrEmpty(token))
                throw new Exception("Token response missing access_token: " + req.downloadHandler.text);

            _cachedToken = token;
            _cachedExpiresUtc = DateTime.UtcNow.AddSeconds(expiresIn);
            _cachedCredentialsPath = credentialsPath;
            return token;
        }

        private static string BuildSignedJwt(ServiceAccountJson creds)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var header = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";
            var claim = JsonConvert.SerializeObject(new
            {
                iss = creds.ClientEmail,
                scope = Scope,
                aud = TokenEndpoint,
                exp = now + 3600,
                iat = now
            });

            var signingInput = $"{Base64Url(Encoding.UTF8.GetBytes(header))}.{Base64Url(Encoding.UTF8.GetBytes(claim))}";
            var signature = SignRs256(signingInput, creds.PrivateKey);
            return $"{signingInput}.{Base64Url(signature)}";
        }

        private static byte[] SignRs256(string input, string privateKeyPem)
        {
            // Unity 6 Mono throws PlatformNotSupportedException on RSA.ImportFromPem AND
            // ImportPkcs8PrivateKey. ImportParameters(RSAParameters) is the only cross-platform
            // path that actually works, so parse the PKCS#8 DER by hand into RSAParameters.
            using var rsa = RSA.Create();
            rsa.ImportParameters(ParsePkcs8RsaPrivateKey(DecodePemToDer(privateKeyPem)));
            return rsa.SignData(Encoding.UTF8.GetBytes(input), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        private static byte[] DecodePemToDer(string pem)
        {
            var sb = new StringBuilder(pem.Length);
            foreach (var line in pem.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                if (trimmed.StartsWith("-----BEGIN")) continue;
                if (trimmed.StartsWith("-----END")) continue;
                sb.Append(trimmed);
            }
            return Convert.FromBase64String(sb.ToString());
        }

        // ---- Minimal ASN.1 DER parsing for PKCS#8 (RSA) -----------------------------------
        //
        // PKCS#8 PrivateKeyInfo:
        //   SEQUENCE {
        //     INTEGER version (0),
        //     SEQUENCE AlgorithmIdentifier { OID rsaEncryption, NULL },
        //     OCTET STRING wrapping RSAPrivateKey
        //   }
        // RSAPrivateKey:
        //   SEQUENCE { INTEGER version, INTEGER n, e, d, p, q, dp, dq, qInv }
        //
        // RSAParameters requires unsigned big-endian bytes; CRT parameters must be padded to
        // n/2 length and D to n length or Mono's RSA rejects them.
        private static RSAParameters ParsePkcs8RsaPrivateKey(byte[] pkcs8)
        {
            int o = 0;
            ExpectTag(pkcs8, ref o, 0x30); ReadLength(pkcs8, ref o);     // outer SEQUENCE
            SkipField(pkcs8, ref o);                                      // version INTEGER
            SkipField(pkcs8, ref o);                                      // AlgorithmIdentifier SEQUENCE
            ExpectTag(pkcs8, ref o, 0x04);                                // OCTET STRING
            int inner = ReadLength(pkcs8, ref o);
            var rsaDer = new byte[inner];
            Array.Copy(pkcs8, o, rsaDer, 0, inner);

            int r = 0;
            ExpectTag(rsaDer, ref r, 0x30); ReadLength(rsaDer, ref r);    // RSAPrivateKey SEQUENCE
            ReadInteger(rsaDer, ref r);                                   // version (discarded)
            var n = ReadInteger(rsaDer, ref r);
            var e = ReadInteger(rsaDer, ref r);
            var d = ReadInteger(rsaDer, ref r);
            var p = ReadInteger(rsaDer, ref r);
            var q = ReadInteger(rsaDer, ref r);
            var dp = ReadInteger(rsaDer, ref r);
            var dq = ReadInteger(rsaDer, ref r);
            var iq = ReadInteger(rsaDer, ref r);

            int nLen = n.Length;
            int halfLen = (nLen + 1) / 2;
            return new RSAParameters
            {
                Modulus = n, Exponent = e,
                D = PadLeft(d, nLen),
                P = PadLeft(p, halfLen), Q = PadLeft(q, halfLen),
                DP = PadLeft(dp, halfLen), DQ = PadLeft(dq, halfLen),
                InverseQ = PadLeft(iq, halfLen)
            };
        }

        private static void ExpectTag(byte[] data, ref int offset, byte expected)
        {
            if (offset >= data.Length || data[offset] != expected)
                throw new InvalidDataException($"ASN.1 parse error at offset {offset}: expected tag 0x{expected:X2}.");
            offset++;
        }

        private static int ReadLength(byte[] data, ref int offset)
        {
            int first = data[offset++];
            if ((first & 0x80) == 0) return first;
            int numBytes = first & 0x7F;
            int length = 0;
            for (int i = 0; i < numBytes; i++) length = (length << 8) | data[offset++];
            return length;
        }

        private static void SkipField(byte[] data, ref int offset)
        {
            offset++; // tag
            int len = ReadLength(data, ref offset);
            offset += len;
        }

        private static byte[] ReadInteger(byte[] data, ref int offset)
        {
            ExpectTag(data, ref offset, 0x02);
            int len = ReadLength(data, ref offset);
            int start = offset, end = offset + len;
            if (len > 1 && data[start] == 0x00) start++; // strip sign byte
            var result = new byte[end - start];
            Array.Copy(data, start, result, 0, result.Length);
            offset = end;
            return result;
        }

        private static byte[] PadLeft(byte[] bytes, int length)
        {
            if (bytes.Length == length) return bytes;
            if (bytes.Length > length) throw new InvalidDataException($"RSA parameter byte length {bytes.Length} exceeds expected {length}.");
            var padded = new byte[length];
            Array.Copy(bytes, 0, padded, length - bytes.Length, bytes.Length);
            return padded;
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        // Only the fields we use from Google's JSON key. Tolerant of extra fields.
        private class ServiceAccountJson
        {
            [JsonProperty("client_email")] public string ClientEmail { get; set; }
            [JsonProperty("private_key")] public string PrivateKey { get; set; }
        }
    }
}
