Shader "EditorVR/Workspaces/WorkspaceRenderTextureOverlay"
{
	Properties
	{
		_Alpha ("Alpha", Range(0, 1)) = 1
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "Queue" = "Overlay+5103" "LightMode" = "Always" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		ZWrite Off
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				half4 vertex : POSITION;
				half2 uv : TEXCOORD0;
			};

			struct v2f
			{
				half2 uv : TEXCOORD0;
				half4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			half4 _MainTex_ST;
			half _Alpha;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				col = col + col * col; // Amplify color
				col.a = _Alpha * (col.r + col.g + col.b);  // Sample alpha as the cumulative r/g/b value of the fragment
				return col;
			}
			ENDCG
		}
	}
}
