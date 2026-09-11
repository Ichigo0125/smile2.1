Shader "Custom/ClearAlphaMask"
{
    Properties
    {
        // 若希望強制穿透所有前方物體，可在 Inspector 中將 ZTest 切換為 Always (8)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4 // 預設 4 為 LEqual
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ClearAlpha"
            
            // 保留原本 RGB (Zero One)，將目標 Alpha 強制設為 Shader 輸出的 Alpha (One Zero)
            Blend Zero One, One Zero
            
            // 僅寫入 Alpha 通道
            ColorMask A
            
            // 不寫入深度緩衝
            ZWrite Off
            ZTest [_ZTest]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 輸出 Alpha = 0，配合 Blend One Zero 將畫面的 Alpha 洗成 0
                return half4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}