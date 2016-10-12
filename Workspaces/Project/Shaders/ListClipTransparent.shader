Shader "Custom/List Clip Transparent"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_ClipExtents("Clip Extents", Vector) = (0,0,0,0)
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent+2" }
		Lighting Off
		LOD 200
		ZWrite Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard alpha:fade vertex:listClipVert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
			float3 localPos;
		};

		#include "ListClip.cginc"

		sampler2D _MainTex;
		half _Glossiness;
		half _Metallic;
		half4 _Color;

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			listClipCheckExtents(IN.localPos);

			half4 c = tex2D(_MainTex, IN.uv_MainTex) *_Color;
			o.Emission = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
