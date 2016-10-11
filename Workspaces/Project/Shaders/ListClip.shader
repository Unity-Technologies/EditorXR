Shader "Custom/List Clip"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_ClipExtents("Clip Extents", Vector) = (0,0,0,0)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue" = "Transparent+1" }
		Lighting Off
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard vertex:listClipVert alpha:auto

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input
		{
			float3 localPos;
		};

		#include "ListClip.cginc"

		half _Glossiness;
		half _Metallic;
		half4 _Color;

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			listClipCheckExtents(IN.localPos);
			o.Emission = _Color.rgb;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
