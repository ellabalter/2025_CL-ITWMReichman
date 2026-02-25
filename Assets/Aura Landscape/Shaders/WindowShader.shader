Shader "VIS/Window" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Cube ("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeReflect }
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
//	ZWrite Off Cull Off
	LOD 300
	
CGPROGRAM
#pragma surface surf Lambert alpha

sampler2D _MainTex;
samplerCUBE _Cube;
fixed4 _Color;
fixed4 _ReflectColor;

struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;
	float3 worldRefl;
	INTERNAL_DATA
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	fixed4 c = tex * _Color;
	
	o.Albedo = c.rgb;

	float3 worldRefl = WorldReflectionVector (IN, o.Normal);
	fixed4 reflcol = texCUBE (_Cube, worldRefl);
	reflcol *= tex.a;
	o.Emission = reflcol.rgb * _ReflectColor.rgb * (1.0f - tex.a);
	//o.Alpha = reflcol.a * _ReflectColor.a * 2.0f;
	o.Alpha = tex.a;
}
ENDCG
}

FallBack "Transparent/Diffuse"
}