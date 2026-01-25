Shader "LegionCore/Simple_Fish_Instanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Speed ("Swim Speed", Float) = 5.0
        _Amplitude ("Wave Amount", Float) = 0.5
        _Frequency ("Wave Frequency", Float) = 2.0
        // [수정 1] 시작 번호를 받을 변수 추가 (Inspector에는 안 보여도 됨)
        [HideInInspector] _BaseIndex ("Base Index", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ConfigureProcedural
            
            // URP용 핵심 라이브러리 포함
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID; // 인스턴싱 필수 ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Speed;
                float _Amplitude;
                float _Frequency;
                float _BaseIndex;
            CBUFFER_END

            struct UnitData
            {
                float4x4 objectToWorld;
                float animOffset;
                float pad1;
                float pad2;
                float pad3;
            };

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<UnitData> _UnitBuffer;
            #endif

            void ConfigureProcedural()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    // 위치 계산은 vert 함수에서 직접 처리합니다.
                #endif
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float4x4 mat = GetObjectToWorldMatrix();
                float animOffset = 0;

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint finalIndex = unity_InstanceID + (uint)_BaseIndex;
                    
                    UnitData data = _UnitBuffer[finalIndex];
                    mat = data.objectToWorld;
                    animOffset = data.animOffset;
                #endif

                float3 pos = v.vertex.xyz;
                float spineAxis = pos.x;
                float mask = abs(spineAxis); 

                float wave = sin(_Time.y * _Speed + spineAxis * _Frequency + animOffset);
                pos.z += wave * _Amplitude * mask;
                float3 worldPos = mul(mat, float4(pos, 1.0)).xyz;
                o.vertex = TransformWorldToHClip(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                return col;
            }
            ENDHLSL
        }
    }
}