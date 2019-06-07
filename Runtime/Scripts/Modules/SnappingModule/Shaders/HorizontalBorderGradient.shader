Shader "EditorVR/MainMenu/HorizontalBorderGradient"
{
	Properties
	{
		_ColorLeft("Left Color", Color) = (1,1,1,1)
		_ColorRight("Right Color", Color) = (1,1,1,1)
		_Alpha ("Alpha", Range(0, 1)) = 1
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags { "Queue"="Overlay+5104" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
		ZWrite Off
		ZTest Less
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _ColorRight;
			fixed4 _ColorLeft;
			half _Alpha;

			struct v2f
			{
				float4 position : SV_POSITION;
				fixed4 color : COLOR;
			};

			v2f vert(appdata_full v)
			{
				v2f output;
				output.position = UnityObjectToClipPos(v.vertex);
				output.color = lerp(_ColorLeft, _ColorRight, v.texcoord.x);
				return output;
			}

			float4 frag(v2f i) : COLOR
			{
				float4 col = i.color;
				col.a = _Alpha;
				return col;
			}
			ENDCG
		}
	}
}