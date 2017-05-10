Shader "EditorVR/RadialMenu/RadialMenuIcons"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "Queue"="Overlay+5503" "IgnoreProjector"="True" "RenderType"="Transparent" "ForceNoShadowCasting"="True" }
		Fog { Mode Off }

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex: POSITION;
				float2 texcoord: TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex: SV_POSITION;
				half2 texcoord: TEXCOORD0;
			};

			sampler2D _MainTex;
			fixed4 _Color;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				half4 color = tex2D(_MainTex, IN.texcoord) * _Color;

				return color;
			}
			ENDCG
		}
	}
}
