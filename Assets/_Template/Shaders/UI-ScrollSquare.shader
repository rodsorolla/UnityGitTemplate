Shader "UI/ScrollSquare"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)

        _Velocity ("Velocity (UV direction XY)", Vector) = (1, 0, 0, 0)
        _Speed    ("Speed (multiplier)", Float) = 1
        _Tiling   ("Tiling (XY)", Vector) = (1, 1, 0, 0)

        // 0 = Cover (crop, no stretching)  |  1 = Fit (letterbox, no stretching)
        _SquareMode ("Square Mode (0=Cover, 1=Fit)", Float) = 0

        // --- Standard Unity UI stencil/clip properties ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPos      : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            bool _UseUIAlphaClip;

            float4 _Velocity;   // use .xy
            float  _Speed;
            float4 _Tiling;     // use .xy
            float  _SquareMode;

            float4 _ClipRect;

            // Computes rect aspect in the same UV-space you're sampling from (robust for UI transforms)
            float ComputeAspectFromDerivatives(float2 uv, float2 localPos)
            {
                float2 duv_dx = ddx(uv);
                float2 duv_dy = ddy(uv);
                float2 dlp_dx = ddx(localPos);
                float2 dlp_dy = ddy(localPos);

                // det of [duv_dx duv_dy] (columns)
                float det = duv_dx.x * duv_dy.y - duv_dx.y * duv_dy.x;
                float absDet = max(abs(det), 1e-8);
                float invDet = (det >= 0.0 ? 1.0 : -1.0) / absDet;

                // dLocal/du and dLocal/dv (solve A in: dLocal = A * dUV)
                float2 dLocal_du = (dlp_dx * duv_dy.y - dlp_dy * duv_dx.y) * invDet;
                float2 dLocal_dv = (-dlp_dx * duv_dy.x + dlp_dy * duv_dx.x) * invDet;

                float sx = max(length(dLocal_du), 1e-6);
                float sy = max(length(dLocal_dv), 1e-6);

                return sx / sy; // width / height in local space
            }

            float2 ApplySquare(float2 uv, float aspect, float mode, out float mask)
            {
                // mode: 0=cover(crop), 1=fit(letterbox)
                float2 scaleCover = (aspect >= 1.0) ? float2(aspect, 1.0) : float2(1.0, 1.0 / aspect);
                float2 scaleFit   = (aspect >= 1.0) ? float2(1.0 / aspect, 1.0) : float2(1.0, aspect);

                float2 scale = lerp(scaleCover, scaleFit, saturate(mode));

                float2 u = (uv - 0.5) * scale + 0.5;

                // For fit mode, outside [0..1] becomes transparent (letterbox).
                // For cover mode, we keep mask = 1 (cropping happens by sampling outside, so use Repeat wrap if desired).
                float isFit = step(0.5, saturate(mode));
                float2 in01 = step(0.0, u) * step(u, 1.0);
                float fitMask = in01.x * in01.y;
                mask = lerp(1.0, fitMask, isFit);

                return u;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.localPos = v.vertex.xy;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float aspect = ComputeAspectFromDerivatives(i.uv, i.localPos);

                float mask;
                float2 uvSq = ApplySquare(i.uv, aspect, _SquareMode, mask);

                float2 uvScrolled =
                    uvSq * _Tiling.xy +
                    (_Velocity.xy * _Speed * _Time.y);

                fixed4 col = (tex2D(_MainTex, uvScrolled) + _TextureSampleAdd) * i.color;
                col.a *= mask;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}