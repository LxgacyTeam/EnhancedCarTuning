Shader "ECT/TireRubberTexture"
{
    Properties
    {
        _MainTex ("Rubber albedo", 2D) = "white" {}
        _Color ("Rubber tint", Color) = (0.65,0.65,0.65,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        // The game's material quality cap ranges from 100 to 400. Always keep
        // the texture-sampling pass available, including the lowest quality.
        LOD 100
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows addshadow
        #pragma target 2.0
        sampler2D _MainTex;
        fixed4 _Color;
        struct Input { float2 uv_MainTex; };
        void surf(Input input, inout SurfaceOutput output)
        {
            output.Albedo = tex2D(_MainTex, input.uv_MainTex).rgb * _Color.rgb;
            output.Alpha = 1;
        }
        ENDCG
    }
    Fallback Off
}
