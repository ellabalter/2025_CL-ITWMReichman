Shader "VIS/Road"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Cutoff("Cutoff", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Offset -1,-5

        CGPROGRAM

        #pragma surface surf Lambert  alphatest:_Cutoff

        float4    _Color;
        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = c.rgb;
            o.Alpha  = c.a;
        }

        ENDCG
    }

    FallBack "Transparent/Cutout/Diffuse"
}
