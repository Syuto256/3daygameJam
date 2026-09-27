// ベルトコンベアのベルト面を流して見せるスプライト用シェーダー。
// テクスチャを V 方向（スプライトのローカル下向き）へ流すだけで、ライティングはしない。
// 流す速さ（UV/秒）は BeltConveyorScroller がレンダラーごとに _ScrollSpeed へ入れる
Shader "GameJam/BeltScroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _ScrollSpeed ("Scroll Speed (UV/sec)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            // SpriteRenderer の色・反転は頂点色ではなく unity_SpriteColor / unity_SpriteProps で渡ってくる
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _ScrollSpeed;
            CBUFFER_END

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                SetUpSpriteInstanceProperties();
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.color = input.color * _Color * unity_SpriteColor;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // テクスチャの Wrap Mode は Repeat にしておく（frac を使うと継ぎ目に線が出る）
                float2 uv = input.uv;
                uv.y += _Time.y * _ScrollSpeed;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * input.color;
            }
            ENDHLSL
        }
    }
}
