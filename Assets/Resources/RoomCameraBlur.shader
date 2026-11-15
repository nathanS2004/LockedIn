Shader "LockedIn/Room Camera Gaussian Blur"
{
    Properties { _MainTex ("Camera RGB", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float2 _BlurStep;
            float4 frag(v2f_img i) : SV_Target
            {
                float3 color = tex2D(_MainTex, i.uv).rgb * 0.227027;
                color += (tex2D(_MainTex, i.uv + _BlurStep).rgb + tex2D(_MainTex, i.uv - _BlurStep).rgb) * 0.1945946;
                color += (tex2D(_MainTex, i.uv + _BlurStep * 2).rgb + tex2D(_MainTex, i.uv - _BlurStep * 2).rgb) * 0.1216216;
                color += (tex2D(_MainTex, i.uv + _BlurStep * 3).rgb + tex2D(_MainTex, i.uv - _BlurStep * 3).rgb) * 0.054054;
                color += (tex2D(_MainTex, i.uv + _BlurStep * 4).rgb + tex2D(_MainTex, i.uv - _BlurStep * 4).rgb) * 0.016216;
                return float4(color, 1);
            }
            ENDCG
        }
    }
}
