Shader "EditorXR/Workspaces/WorkspaceBackgroundBorderGradient"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1,1,1,1)
		_ColorBottom("Bottom Color", Color) = (1,1,1,1)
		_Alpha("Alpha", Range(0, 1)) = 0
	}

	SubShader
	{
		Tags { "Queue"="Overlay+5001" "LightMode" = "Always" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "RenderType" = "Transparent" }
		
		Pass
		{
			ZWrite Off
			Lighting Off
			Cull Off
			ZTest LEqual
			Fog{ Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _ColorTop;
			fixed4 _ColorBottom;
			float _Expand;
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