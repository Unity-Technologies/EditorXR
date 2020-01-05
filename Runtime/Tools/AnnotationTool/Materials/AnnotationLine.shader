Shader "EditorXR/Annotation/Line"
 {
	Properties
	{
		_Texture ("Texture", 2D) = "white" {}
		_EmissionColor ("Color", Color) = (0.5, 0.5, 0.5, 1)
	}

	SubShader
	{
		Tags
		{
			"IgnoreProjector"="True"
			"Queue"="Transparent"
			"RenderType"="Transparent"
			"LightMode" = "Always"
		}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			AlphaToMask On
			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 _EmissionColor;
			sampler2D _Texture;
			float4 _Texture_ST;

			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
			};

			struct VertexOutput
			{
				float4 pos : SV_POSITION;
				half2 uv0 : TEXCOORD0;
			};

			VertexOutput vert (VertexInput i)
			{
				VertexOutput o = (VertexOutput)0;
				o.uv0 = i.texcoord0;
				o.pos = UnityObjectToClipPos(i.vertex);
				return o;
			}

			float4 frag(VertexOutput i) : COLOR
			{
				half4 color = tex2D(_Texture, TRANSFORM_TEX(i.uv0, _Texture));
				color.rgb = _EmissionColor.rgb * color.rgb;
				color.a *= _EmissionColor.a;
				return color;
			}

			ENDCG
		}
	}

	FallBack "Diffuse"
}
