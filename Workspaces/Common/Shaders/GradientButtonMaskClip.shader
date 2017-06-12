Shader "EditorVR/UI/GradientButtonMaskClip"
{
	Properties
	{
		_StencilRef("StencilRef", Int) = 3
		_ClipExtents("Clip Extents", Vector) = (1,1,1,0)
	}

	SubShader
	{
		Tags { "Queue"="Overlay+5105" "LightMode" = "Always" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite On
		ZTest LEqual
		ColorMask 0

		Stencil
		{
			Ref[_StencilRef]
			Pass replace
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4x4 _ParentMatrix;
			float4 _ClipExtents;

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 localPosition : TEXCOORD2;
			};

			v2f vert(appdata_full v)
			{
				v2f output;
				output.position = UnityObjectToClipPos(v.vertex);
				output.localPosition = mul(_ParentMatrix, mul(UNITY_MATRIX_M, v.vertex));
				return output;
			}

			half4 frag(v2f input) : COLOR
			{
				float3 diff = abs(input.localPosition);
				if (diff.x > _ClipExtents.x || diff.y > _ClipExtents.y || diff.z > _ClipExtents.z)
					discard;

				return 0;
			}
			ENDCG
		}
	}
}
