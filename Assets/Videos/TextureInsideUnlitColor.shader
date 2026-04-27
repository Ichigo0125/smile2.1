Shader "Unlit/VR360Stereo"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [KeywordEnum(None, SideBySide, OverUnder)] _StereoLayout ("Stereo Layout", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha 
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // ⭐ 支援 VR instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _STEREOLAYOUT_NONE _STEREOLAYOUT_SIDEBYSIDE _STEREOLAYOUT_OVERUNDER

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 原本的水平翻轉（inside sphere）
                uv.x = 1 - uv.x;

                // ⭐ 判斷左右眼（0 = 左眼, 1 = 右眼）
                int eyeIndex = unity_StereoEyeIndex;

                #if defined(_STEREOLAYOUT_SIDEBYSIDE)
                    uv.x = uv.x * 0.5 + eyeIndex * 0.5;
                #elif defined(_STEREOLAYOUT_OVERUNDER)
                    uv.y = uv.y * 0.5 + (1 - eyeIndex) * 0.5;
                #endif

                fixed4 col = tex2D(_MainTex, uv) * _Color;
                return col;
            }

            ENDCG
        }
    }
}