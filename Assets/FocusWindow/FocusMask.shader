Shader "LockedIn/Focus Mask"
{
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            // Premultiplied output preserves the camera's alpha for the passthrough underlay.
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float3 _WindowOrigin, _WindowRight, _WindowUp, _WindowNormal;
            float3 _WindowMin, _WindowMax, _Probe, _BackgroundColor;
            float _Dimming, _HasWindow, _Preview;

            float4 Background(float opacity)
            {
                return float4(_BackgroundColor * opacity, opacity);
            }

            struct AppData
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(AppData input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_OUTPUT(Varyings, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.vertex);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }

            float WindowCoverage(float3 eye, float3 ray)
            {
                float3 relativeEye = eye - _WindowOrigin;
                float3 origin = float3(dot(relativeEye, _WindowRight), dot(relativeEye, _WindowUp),
                                       dot(relativeEye, _WindowNormal));
                float3 direction = float3(dot(ray, _WindowRight), dot(ray, _WindowUp),
                                          dot(ray, _WindowNormal));

                // Slab intersection. Keep near-parallel rays finite while retaining their sign.
                float3 slope = lerp(-1.0, 1.0, step(0.0, direction)) * max(abs(direction), 0.000001);
                float3 entry = (_WindowMin - origin) / slope;
                float3 exit = (_WindowMax - origin) / slope;
                float3 nearSide = min(entry, exit);
                float3 farSide = max(entry, exit);
                float nearHit = max(0.0, max(nearSide.x, max(nearSide.y, nearSide.z)));
                float farHit = min(farSide.x, min(farSide.y, farSide.z));
                return smoothstep(-0.006, 0.0, farHit - nearHit);
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                if (_HasWindow < 0.5 && _Preview < 0.5)
                    return Background(_Dimming);

                float3 eye = _WorldSpaceCameraPos;
                float3 ray = normalize(input.worldPosition - eye);
                if (_Preview > 0.5)
                {
                    float3 delta = _Probe - eye;
                    float depth = dot(delta, ray);
                    float markerDistance = length(delta - depth * ray);
                    float alpha = depth > 0.0 ? 1.0 - smoothstep(0.003, 0.006, markerDistance) : 0.0;
                    return float4(float3(0.12, 1.0, 0.75) * alpha, alpha);
                }

                return Background(_Dimming * (1.0 - WindowCoverage(eye, ray)));
            }
            ENDCG
        }
    }
}
