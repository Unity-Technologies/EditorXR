Shader "EditorVR/MainMenu/VerticalBorderGradient"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1,1,1,1)
		_ColorBottom("Bottom Color", Color) = (1,1,1,1)
		_Alpha ("Alpha", Range(0, 1)) = 1
	}

	SubShader
	{
		Tags { "Queue"="Transparent-3" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
		ZWrite Off
		ZTest Less
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _ColorTop;
			fixed4 _ColorBottom;
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
				output.color = lerp(_ColorBottom, _ColorTop, v.texcoord.y);
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