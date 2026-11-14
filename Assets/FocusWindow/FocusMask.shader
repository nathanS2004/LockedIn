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

            float3 _PlaneOrigin, _PlaneNormal, _PlaneRight, _PlaneUp;
            float4 _Window, _Aim;
            float _Dimming, _Preview, _ShowBounds;

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

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 eye = _WorldSpaceCameraPos;
                float3 ray = normalize(input.worldPosition - eye);
                float alignment = dot(ray, _PlaneNormal);
                if (alignment <= 0.0001)
                    return float4(0, 0, 0, _Dimming);

                float depth = dot(_PlaneOrigin - eye, _PlaneNormal) / alignment;
                if (depth <= 0)
                    return float4(0, 0, 0, _Dimming);

                float3 offset = eye + ray * depth - _PlaneOrigin;
                float2 uv = float2(dot(offset, _PlaneRight), dot(offset, _PlaneUp));
                float2 edges = max(_Window.xy - uv, uv - _Window.zw);
                float edge = max(edges.x, edges.y);
                float dim = _Dimming * smoothstep(0.0, 0.012, edge);
                float dotAlpha = 1.0 - smoothstep(0.003, 0.006, distance(uv, _Aim.xy));
                float lineAlpha = _ShowBounds * (1.0 - smoothstep(0.001, 0.004, abs(edge)));
                float preview = _Preview * max(dotAlpha, lineAlpha);
                return float4(float3(0.12, 1.0, 0.75) * preview, max(preview, dim));
            }
            ENDCG
        }
    }
}
