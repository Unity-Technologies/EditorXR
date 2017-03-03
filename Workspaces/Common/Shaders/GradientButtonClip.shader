Shader "EditorVR/UI/GradientButtonClip"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1,1,1,1)
		_ColorBottom("Bottom Color", Color) = (1,1,1,1)
		_Alpha("Alpha", Range(0.0, 1.0)) = 1.0
		_StencilRef("StencilRef", Int) = 3
		_ClipExtents("Clip Extents", Vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags{ "Queue" = "Overlay+5101" "LightMode" = "Always" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "RenderType" = "Transparent" }
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		Stencil
		{
			Ref[_StencilRef]
			Comp equal
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _ColorTop;
			fixed4 _ColorBottom;
			fixed _Alpha;
			float4 _ClipRect;

			float4x4 _ParentMatrix;
			float4 _ClipExtents;

			struct v2f
			{
				float4 position : SV_POSITION;
				fixed4 color : COLOR;
				float4 localPosition : TEXCOORD2;
			};

			v2f vert(appdata_full v)
			{
				v2f output;
				output.position = mul(UNITY_MATRIX_MVP, v.vertex);
				output.color = lerp(_ColorBottom, _ColorTop, v.texcoord.y);
				output.localPosition = mul(_ParentMatrix, mul(UNITY_MATRIX_M, v.vertex));
				return output;
			}

			half4 frag(v2f input) : COLOR
			{
				float3 diff = abs(input.localPosition);
				if (diff.x > _ClipExtents.x || diff.y > _ClipExtents.y || diff.z > _ClipExtents.z)
					discard;

				half4 col = input.color;
				col.a = _Alpha;
				return col;
			}
			ENDCG
		}
	}
}