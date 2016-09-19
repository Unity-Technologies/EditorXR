Shader "EditorVR/RadialMenu/RadialMenuSlotMask"
{
	SubShader
	{
		Tags { "Queue"="Geometry" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite On
		ZTest NotEqual
		Blend Zero OneMinusSrcAlpha

		Stencil{
			Ref 1
			Comp always
			Pass replace
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 position : SV_POSITION;
			};

			v2f vert(appdata_full v)
			{
				v2f output;
				output.position = mul(UNITY_MATRIX_MVP, v.vertex);
				return output;
			}

			float4 frag(v2f input) : COLOR
			{
				return 0;
			}
			ENDCG
		}
	}
}