Shader "VIS/Simple Water Animated"
{
    Properties
    {
        _MainColor("Main Color (RGB) Opacity (A)", Color) = (1, 1, 1, 1)
        _SpecColor("Specular Color (RGB)", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess("Shininess", Range(0.01, 1)) = 0.1
        _EmissionFactor("Emission Factor", Range(0, 1)) = 1
        _ReflIntensity("Refl. Intensity", Range(0, 1)) = 0.5
        _BumpReflStr("Bump Reflection Strength", Range(0, 2)) = 0.5
        _ShoreBlending("Shore Blending", Range(0, 1)) = 0.2
        _RippleSpeed("Ripple Speed", Range(0, 10)) = 4
        _Scrolling("Scrolling", Vector) = (-1, 0, 0.1, 0)
        _Waves("Waves", Vector) = (0, 0, 0, 0)
        _MainTex("Base (RGB) Refl. Gloss (A)", 2D) = "white" {}
        _ReflectionTex("Reflection Texture", CUBE) = "white" {}
        _BumpMap("Bump Map", 2D) = "bump" {}
    }

    SubShader 
    {   
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        CGPROGRAM
        
        #pragma surface surf BlinnPhong alpha vertex:vert approxview halfasview noforwardadd
        #pragma only_renderers d3d9 d3d11 opengl
        #pragma target 3.0
        #pragma debug

        sampler2D   _MainTex;
        sampler2D   _BumpMap;
        samplerCUBE _ReflectionTex;
        sampler2D   _CameraDepthTexture;

        float4 _MainColor;
        float  _Shininess;
        float  _EmissionFactor;
        float  _ReflIntensity;
        float  _BumpReflStr;
        float  _RippleSpeed;
        float  _ShoreBlending;
        float  _Opacity;
        float4 _Scrolling;
        float4 _Waves;
        
        float4 _CameraDepthTexture_TexelSize;

        struct Input 
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;            
            float3 worldRefl; 
            float4 screenPos;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float2 dir = normalize(_Scrolling.xy);
            float2 ofs = dir * _Scrolling.z * _Time.y / 100.0;
            
            v.texcoord.xy  += ofs;
            
            float x = sin(_Time.y * v.texcoord.x * dir.x * _Waves.x);
            float y = cos(_Time.y * v.texcoord.y * dir.y * _Waves.y);
            
            v.vertex.y += (x + y) * _Waves.z;            
        } 

        void surf(Input IN, inout SurfaceOutput OUT) 
        {
            float z1, z2;
            
            z1 = tex2Dproj(_CameraDepthTexture,  IN.screenPos).r;
            z1 = LinearEyeDepth(z1); 
            z2 = IN.screenPos.z;
            
            float waterDepth = _ShoreBlending * abs(z2 - z1);
            
            float2 bumpOfs = _RippleSpeed * _Time.y * _CameraDepthTexture_TexelSize.xy;
            
            OUT.Normal = 0.5 * (
                UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap + bumpOfs)) +
                UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap - bumpOfs)));

            fixed3 worldRefl = WorldReflectionVector(IN, OUT.Normal * float3(_BumpReflStr, _BumpReflStr, _BumpReflStr));
            fixed4 reflColor = texCUBE(_ReflectionTex, worldRefl) * _ReflIntensity;
            
            fixed4 waterColor = tex2D(_MainTex, IN.uv_MainTex) * _MainColor;

            waterColor.a *= waterDepth;
            waterColor.rgb += reflColor.rgb;            
            waterColor = saturate(waterColor);
            
            OUT.Albedo    = waterColor.rgb;
            OUT.Alpha     = waterColor.a;
            OUT.Specular  = _Shininess;
            OUT.Emission  = OUT.Albedo * _EmissionFactor;
        }
        
        ENDCG
    }

    FallBack Off
}
