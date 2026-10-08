Shader "Custom/UIAuraShield"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [HDR] _AuraColor ("Aura Color", Color) = (0.25, 1, 0.5, 1)
        _CoreIntensity ("Core Intensity", Range(0, 4)) = 1.2
        _GlowWidth ("Glow Width", Range(0, 0.25)) = 0.06
        _GlowIntensity ("Glow Intensity", Range(0, 8)) = 2.5
        _GlowFalloff ("Glow Falloff", Range(0.2, 6)) = 1.8
        _RimWidth ("Rim Width", Range(0, 0.1)) = 0.018
        _RimIntensity ("Rim Intensity", Range(0, 8)) = 2.0

        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.35
        _NoiseScale ("Noise Scale", Range(0.5, 30)) = 7
        _NoiseAmount ("Noise Amount", Range(0, 1)) = 0.55
        _FlowSpeed ("Flow Speed (XY)", Vector) = (0.06, -0.35, 0, 0)
        _Distortion ("Distortion", Range(0, 0.08)) = 0.012
        _Alpha ("Alpha", Range(0, 1)) = 1

        // RectTransform の 横幅/高さ。グローの太さを縦横で揃えるために使う
        _Aspect ("Rect Aspect (W/H)", Float) = 1

        // 出力は乗算済みアルファ。加算 = One/One、通常 = One/OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1  // One
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1  // One（加算）

        // --- uGUI 標準プロパティ ---
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
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            Name "AURA"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
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
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _AuraColor;
            half _CoreIntensity;
            half _GlowWidth;
            half _GlowIntensity;
            half _GlowFalloff;
            half _RimWidth;
            half _RimIntensity;
            half _PulseSpeed;
            half _PulseAmount;
            half _NoiseScale;
            half _NoiseAmount;
            float4 _FlowSpeed;
            half _Distortion;
            half _Alpha;
            float _Aspect;

            // --- 手続き型ノイズ（ノイズテクスチャ不要） ---
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = vnoise(p) * 0.5;
                v += vnoise(p * 2.03) * 0.3;
                v += vnoise(p * 4.11) * 0.2;
                return v;
            }

            // uv を中心に半径 r（UV単位）の 8 方向でアルファを平均する
            float RingAlpha(float2 uv, float2 r)
            {
                const float k = 0.70710678;
                float s = 0;
                s += tex2D(_MainTex, uv + float2( r.x, 0)).a;
                s += tex2D(_MainTex, uv + float2(-r.x, 0)).a;
                s += tex2D(_MainTex, uv + float2(0,  r.y)).a;
                s += tex2D(_MainTex, uv + float2(0, -r.y)).a;
                s += tex2D(_MainTex, uv + float2( r.x * k,  r.y * k)).a;
                s += tex2D(_MainTex, uv + float2(-r.x * k,  r.y * k)).a;
                s += tex2D(_MainTex, uv + float2( r.x * k, -r.y * k)).a;
                s += tex2D(_MainTex, uv + float2(-r.x * k, -r.y * k)).a;
                return s * 0.125;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float t = _Time.y;

                // UV 上のオフセットを縦横で同じ見た目の太さにするための補正
                float2 px = float2(1.0, max(_Aspect, 0.0001));

                // 流れるノイズ（エネルギーのゆらぎ）
                float n = fbm(uv * _NoiseScale + _FlowSpeed.xy * t);
                float energy = lerp(1.0, 0.45 + n * 1.3, _NoiseAmount);

                // 脈動
                float pulse = 1.0 + sin(t * _PulseSpeed) * _PulseAmount;

                // 本体。ノイズでわずかに揺らす
                float2 duv = uv + (n - 0.5) * _Distortion * px;
                half4 tex = tex2D(_MainTex, duv) + _TextureSampleAdd;
                float a = tex.a;

                // 外側ハロー：アルファを膨張させて本体の外だけ残す
                float2 gr = _GlowWidth * px;
                float d1 = RingAlpha(uv, gr * 0.45);
                float d2 = RingAlpha(uv, gr);
                float halo = saturate(pow(saturate(d1 * 0.55 + d2 * 0.45), _GlowFalloff));
                float outer = saturate(halo - a);

                // 内側リム：アルファを収縮させて縁だけ残す
                float er = RingAlpha(uv, _RimWidth * px);
                float rim = a * saturate(1.0 - er);

                float aura = (outer * _GlowIntensity + rim * _RimIntensity) * pulse * energy;

                // 乗算済みアルファで合成する（本体は a、オーラは aura が強度）
                half3 rgb = tex.rgb * a * _CoreIntensity * lerp(1.0, energy * pulse, 0.5)
                          + _AuraColor.rgb * aura;
                half alpha = saturate(a + aura * _AuraColor.a);

                half fade = IN.color.a * _Alpha;

                #ifdef UNITY_UI_CLIP_RECT
                fade *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                half4 color;
                color.rgb = rgb * IN.color.rgb * fade;
                color.a = alpha * fade;

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
