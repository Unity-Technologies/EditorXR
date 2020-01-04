Shader "EditorXR/RadialMenu/RadialMenuBorderGradient"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1,1,1,1)
		_ColorBottom("Bottom Color", Color) = (1,1,1,1)
		_Expand("Expand", Range(0.0, 1.0)) = 0
	}

	SubShader
	{
		Tags { "Queue"="Geometry-1" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
		
		Pass
		{
			ZWrite On
			Lighting Off
			Cull Front
			ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _ColorTop;
			fixed4 _ColorBottom;
			float _Expand;

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
				col.a = 1;
				return col;
			}
			ENDCG
		}

		Pass
			{
				Tags{ "Queue" = "Geometry-2" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }

				ZWrite On
				Offset 10, 5
				Cull Front
				ZTest LEqual
				Blend SrcAlpha OneMinusSrcAlpha

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				fixed4 _ColorTop;
				fixed4 _ColorBottom;
				float _Expand;

				struct v2f
				{
					float4 position : SV_POSITION;
					fixed4 color : COLOR;
				};

				v2f vert(appdata_full v)
				{
					v2f output;
					v.vertex.xyz += v.normal * (_Expand * 0.0125) * 2;
					output.position = UnityObjectToClipPos(v.vertex);
					output.color = lerp(_ColorBottom, _ColorTop, v.texcoord.y);
					return output;
				}

				float4 frag(v2f i) : COLOR
				{
					float4 col = i.color;
					col.a = 1 - _Expand * 80 * 0.0125;
					return col;
				}
				ENDCG
			}
	}
}