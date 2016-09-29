Shader "EditorVR/Workspaces/WorkspaceBackgroundMask"
{
	SubShader
	{
		Tags{ "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "RenderType" = "TransparentCutout" }
		ZWrite On
		Blend Zero One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f
			{
				half4 position : SV_POSITION;
			};

			v2f vert(appdata_base v)
			{
				v2f output;
				output.position = mul(UNITY_MATRIX_MVP, v.vertex);
				return output;
			}

			fixed4 frag(v2f input) : COLOR
			{
				return (0); // Write transparent
			}
			ENDCG
		}
	}
}
