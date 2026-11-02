Shader "ECT/GlassPrototypeFinish"
{
    Properties
    {
        _Color ("Glass color and opacity", Color) = (0,0,0,0)
        _Gloss ("Smoothness", Range(0,1)) = 0.6
        _Metallic ("Metallic", Range(0,1)) = 0
        _CarWindowState ("Window damage and interior visibility", Vector) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Cull Back
        ZWrite Off
        CGPROGRAM
        #pragma surface surf StandardSpecular alpha:premul fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        float4 _CarWindowState;
        half _Gloss, _Metallic;
        struct Input { float3 worldPos; };
        void surf(Input input, inout SurfaceOutputStandardSpecular output)
        {
            clip(.5 - _CarWindowState.g);
            output.Albedo = _Color.rgb * (1 - _Metallic);
            output.Specular = lerp(half3(.04,.04,.04), _Color.rgb, _Metallic) * _Gloss;
            output.Smoothness = _Gloss;
            output.Alpha = _Color.a * (_CarWindowState.a > .01 ? _CarWindowState.a : 1);
        }
        ENDCG
    }
    Fallback Off
}
