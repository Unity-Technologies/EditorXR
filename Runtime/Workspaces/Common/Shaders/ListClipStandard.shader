Shader "EditorVR/Custom/List Clip Standard"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_ClipExtents("Clip Extents", Vector) = (1,1,1,0)
		_StencilRef("StencilRef", Int) = 3
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue" = "Overlay+5103" }
		ZWrite On
		LOD 200

		Stencil
		{
			Ref[_StencilRef]
			Comp NotEqual
		}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard vertex:vert nofog

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
		fixed4 _Color;

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.localPos = listClipLocalPos(v.vertex);
		}

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			listClipFrag(IN.localPos);

			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			c *= _Color * c.a;
			o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
