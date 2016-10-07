Shader "EditorVR/Workspaces/WorkspaceBackgroundMask"
{
	SubShader
	{
		Tags{ "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "LightMode" = "Always" "Queue" = "Transparent-1" "RenderType" = "TransparentCutout" }
		ZWrite On
		Blend Zero One // Dont draw any color data

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
