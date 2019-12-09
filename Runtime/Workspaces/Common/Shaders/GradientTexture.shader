Shader "EditorXR/UI/GradientTexture"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1,1,1,1)
		_ColorBottom("Bottom Color", Color) = (1,1,1,1)
		_Alpha("Alpha", Range(0.0, 1.0)) = 1.0
		_MainTex("Tex2D", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "Queue" = "Overlay+5104" "LightMode" = "Always" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "RenderType" = "Transparent" }
		ZWrite Off
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _ColorTop;
			fixed4 _ColorBottom;
			fixed _Alpha;
			sampler2D _MainTex;
			float4 _MainTex_ST;

			struct v2f
			{
				float4 position : SV_POSITION;
				fixed4 color : COLOR;
				half2 uv : TEXCOORD0;
			};

			v2f vert(appdata_full v)
			{
				v2f output;
				output.position = UnityObjectToClipPos(v.vertex);
				output.color = lerp(_ColorBottom, _ColorTop, v.texcoord.y);
				output.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				return output;
			}


			half4 frag(v2f input) : COLOR
			{
				half4 tex = tex2D(_MainTex, input.uv);
				half4 col = input.color;
				col.a = _Alpha;
				return col * tex;
			}
			ENDCG
		}
	}
}