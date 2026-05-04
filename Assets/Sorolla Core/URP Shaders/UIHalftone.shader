Shader "UI/Halftone"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Halftone Settings)]
        _ColorA ("Color A (Background)", Color) = (1, 1, 1, 1)
        _ColorB ("Color B (Dots)", Color) = (0, 0, 0, 1)
        _Density ("Dot Density", Range(5, 300)) = 40
        _Angle ("Pattern Angle (degrees)", Range(0, 360)) = 45
        _Softness ("Dot Softness", Range(0, 0.5)) = 0.05

        [Header(Gradient Direction)]
        _GradientAngle ("Gradient Angle (degrees)", Range(0, 360)) = 0
        _GradientOffset ("Gradient Offset", Range(-1, 1)) = 0
        _GradientScale ("Gradient Scale", Range(0.1, 5)) = 1

        [Header(Overlay Image)]
        _OverlayTex ("Overlay Texture", 2D) = "black" {}
        _OverlayTint ("Overlay Tint", Color) = (1, 1, 1, 1)
        [KeywordEnum(Off, Multiply, Screen)] _OverlayMode ("Overlay Mode", Float) = 0

        [Header(Noise)]
        _NoiseScale ("Noise Scale", Range(0.1, 20)) = 5
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0
        _NoiseAnimSpeed ("Noise Evolution Speed", Range(0, 10)) = 0
        _NoiseScrollX ("Noise Scroll X", Range(-5, 5)) = 0
        _NoiseScrollY ("Noise Scroll Y", Range(-5, 5)) = 0

        [Header(Animation)]
        _AnimSpeed ("Animation Speed", Range(0, 10)) = 0
        _AnimScrollX ("Scroll Direction X", Range(-1, 1)) = 1
        _AnimScrollY ("Scroll Direction Y", Range(-1, 1)) = 0

        // UI Stencil / Masking support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _ColorA;
            fixed4 _ColorB;
            float _Density;
            float _Angle;
            float _Softness;
            float _GradientAngle;
            float _GradientOffset;
            float _GradientScale;

            sampler2D _OverlayTex;
            float4 _OverlayTex_ST;
            fixed4 _OverlayTint;
            float _OverlayMode;

            float _NoiseScale;
            float _NoiseStrength;
            float _NoiseAnimSpeed;
            float _NoiseScrollX;
            float _NoiseScrollY;

            float _AnimSpeed;
            float _AnimScrollX;
            float _AnimScrollY;

            // 3D hash-based gradient noise — time as third dimension for smooth in-place evolution
            float3 noiseHash3(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float gradientNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(dot(noiseHash3(i + float3(0,0,0)), f - float3(0,0,0)),
                              dot(noiseHash3(i + float3(1,0,0)), f - float3(1,0,0)), u.x),
                         lerp(dot(noiseHash3(i + float3(0,1,0)), f - float3(0,1,0)),
                              dot(noiseHash3(i + float3(1,1,0)), f - float3(1,1,0)), u.x), u.y),
                    lerp(lerp(dot(noiseHash3(i + float3(0,0,1)), f - float3(0,0,1)),
                              dot(noiseHash3(i + float3(1,0,1)), f - float3(1,0,1)), u.x),
                         lerp(dot(noiseHash3(i + float3(0,1,1)), f - float3(0,1,1)),
                              dot(noiseHash3(i + float3(1,1,1)), f - float3(1,1,1)), u.x), u.y),
                    u.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                // Screen-space pixel position for aspect-correct dots
                float4 clipPos = o.vertex;
                o.screenPos = (clipPos.xy / clipPos.w) * 0.5 + 0.5;
                o.screenPos.x *= _ScreenParams.x;
                o.screenPos.y *= _ScreenParams.y;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample sprite texture
                half4 texColor = tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;

                // --- Gradient value (0..1) across the element ---
                float gradRad = _GradientAngle * 0.0174533;
                float2 gradDir = float2(cos(gradRad), sin(gradRad));
                float2 centered = i.texcoord - 0.5;
                float gradientT = saturate((dot(centered, gradDir) + 0.5) * _GradientScale + _GradientOffset);

                // --- Halftone dot pattern ---
                // Use screen-space pixels so dots are always circular
                float patRad = _Angle * 0.0174533;
                float cosA = cos(patRad);
                float sinA = sin(patRad);
                float2 uv = i.screenPos / (_ScreenParams.y / _Density);

                // Animation: scroll the pattern over time
                uv += _Time.y * _AnimSpeed * float2(_AnimScrollX, _AnimScrollY);

                float2 rotUV = float2(
                    uv.x * cosA - uv.y * sinA,
                    uv.x * sinA + uv.y * cosA
                );

                // Distance from nearest dot center (grid cell center)
                float2 cell = frac(rotUV) - 0.5;
                float dist = length(cell);

                // Dot radius driven by gradient — 0.85 overshoots cell diagonal to ensure full coverage at max gradient
                float radius = gradientT * 0.85;

                // Noise: offset radius for irregular/organic dots
                // XY scroll moves the noise pattern spatially, Z evolves it in-place
                float2 noiseUV = rotUV * _NoiseScale + _Time.y * float2(_NoiseScrollX, _NoiseScrollY);
                float noise = gradientNoise3D(float3(noiseUV, _Time.y * _NoiseAnimSpeed)) * _NoiseStrength;
                radius = saturate(radius + noise * 0.5);

                // Smooth step for anti-aliased dot edges
                float dotMask = 1.0 - smoothstep(radius - _Softness, radius + _Softness, dist);

                // Lerp between the two colors based on dot mask
                fixed4 halfColor = lerp(_ColorA, _ColorB, dotMask);

                // Overlay image blend
                fixed4 overlay = tex2D(_OverlayTex, TRANSFORM_TEX(i.texcoord, _OverlayTex)) * _OverlayTint;
                // Mode 0 = Off, 1 = Multiply, 2 = Screen
                if (_OverlayMode > 1.5)
                {
                    // Screen: 1 - (1 - base) * (1 - overlay)
                    halfColor.rgb = 1.0 - (1.0 - halfColor.rgb) * (1.0 - overlay.rgb * overlay.a);
                }
                else if (_OverlayMode > 0.5)
                {
                    // Multiply
                    halfColor.rgb = lerp(halfColor.rgb, halfColor.rgb * overlay.rgb, overlay.a);
                }

                // Combine with sprite texture and vertex color
                fixed4 color = halfColor * texColor * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                // Premultiply alpha
                color.rgb *= color.a;

                return color;
            }
            ENDCG
        }
    }
}
