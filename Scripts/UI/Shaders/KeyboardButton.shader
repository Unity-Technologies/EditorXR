Shader "EditorVR/Keyboard/Button"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1,1,1,1)
		_ColorBottom("Bottom Color", Color) = (1,1,1,1)
		_Alpha("Alpha", Range(0.0, 1.0)) = 1.0
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent+1" "LightMode" = "Always" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "RenderType" = "Transparent" }
		ZWrite On
		ZTest Always
		Fog {Mode Off}
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha

		Stencil
		{
			Ref 1
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

			half4 frag(v2f input) : COLOR
			{
				half4 col = input.color;
				col.a = _Alpha;
				return col;
			}
			ENDCG
		}
	}
}