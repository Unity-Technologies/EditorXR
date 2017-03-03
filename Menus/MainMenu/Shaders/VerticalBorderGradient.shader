Shader "EditorVR/MainMenu/VerticalBorderGradient"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1,1,1,1)
		_ColorBottom("Bottom Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags { "Queue"="Transparent-3" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
		ZWrite Off
		Cull Back

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _ColorTop;
			fixed4 _ColorBottom;

			struct v2f
			{
				float4 position : SV_POSITION;
				fixed4 color : COLOR;
			};

			v2f vert(appdata_full v)
			{
				v2f output;
				output.position = mul(UNITY_MATRIX_MVP, v.vertex);
				output.color = lerp(_ColorBottom, _ColorTop, v.texcoord.y);
				return output;
			}

			float4 frag(v2f i) : COLOR
			{
				float4 col = i.color;
				col.a = 1;
				return col;
			}
			ENDCG
		}
	}
}