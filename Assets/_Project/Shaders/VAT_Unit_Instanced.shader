Shader "LegionCore/VAT_Unit_Instanced"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [NoScaleOffset] _PosTex ("Position Texture (VAT)", 2D) = "black" {}
        _AnimSpeed ("Animation Speed", Float) = 30.0
        _TotalFrames ("Total Frames", Float) = 60.0
        _VertexCount ("Vertex Count (Per Unit)", Float) = 0.0
        [Toggle] _UseVAT ("Enable VAT", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct UnitData
            {
                float4x4 objectToWorld;
                float animOffset;
                float pad1;
                float pad2;
                float pad3;
            };
            StructuredBuffer<UnitData> _UnitBuffer;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AnimSpeed;
                float _TotalFrames;
                float _VertexCount;
                float _UseVAT;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_PosTex);  SAMPLER(sampler_PosTex);

            void setup()
            {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                uint instanceID = unity_InstanceID;
                float4x4 mat = _UnitBuffer[instanceID].objectToWorld;
                unity_ObjectToWorld = mat;
                unity_WorldToObject = mat;
            #endif
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 finalPos = input.positionOS.xyz;

                if (_UseVAT > 0.5 && _VertexCount > 0)
                {
                    float timeOffset = 0;
                    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                        timeOffset = _UnitBuffer[unity_InstanceID].animOffset;
                    #endif

                    float currentTime = (_Time.y * _AnimSpeed) + timeOffset;
                    float currentFrame = fmod(currentTime, _TotalFrames);

                    float u = (float(input.vertexID) + 0.5) / _VertexCount;
                    float v = (currentFrame + 0.5) / _TotalFrames;
                    
                    float4 bakedPos = SAMPLE_TEXTURE2D_LOD(_PosTex, sampler_PosTex, float2(u, v), 0);
                    finalPos = bakedPos.rgb;
                }

                float3 positionWS = TransformObjectToWorld(finalPos);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            }
            ENDHLSL
        }
    }
}