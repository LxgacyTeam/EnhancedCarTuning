Shader "ECT/GlassPrototypeOverlay"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,0)
        _MainTex ("Vinyl", 2D) = "white" {}
        _Projected ("Projected", Float) = 0
        _Invert ("Invert", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Back
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color, _DecalSize, _DecalForward;
            float4x4 _WorldToDecal;
            float _Projected, _Invert;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 position : SV_POSITION; float3 decal : TEXCOORD0; float3 normal : TEXCOORD1; };
            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.decal = mul(_WorldToDecal, mul(unity_ObjectToWorld, input.vertex)).xyz;
                output.normal = UnityObjectToWorldNormal(input.normal);
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                if (_Projected < .5) return _Color;
                float2 uv = input.decal.xy / _DecalSize.xy + .5;
                clip(uv); clip(1 - uv);
                clip(input.decal.z + 1); clip(_DecalSize.z - input.decal.z);
                clip(-dot(normalize(input.normal), _DecalForward.xyz) - .05);
                if (_DecalSize.w < 0) uv.x = 1 - uv.x;
                fixed4 pixel = tex2D(_MainTex, uv);
                if (_Invert > .5) pixel.rgb = 1 - pixel.rgb;
                return pixel * _Color;
            }
            ENDCG
        }
    }
}
