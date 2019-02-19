Shader "EditorVR/Custom/Snapping Visuals"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_FadeCenter("Fade Center", Vector) = (1,1,1,1)
		_InnerRadius("Inner Radius", Float) = 0.3
		_FadeDistance("Fade Distance", Float) = 0.4
	}

	SubShader
	{
		Tags{"RenderType" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest Off
		Cull Off
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		//#pragma surface surf NoLighting nolightmap noforwardadd noshadow nometa vertex:vert
		#pragma surface surf Standard vertex:vert alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		sampler2D _MainTex;

		struct Input
		{
			float2 uv_MainTex;
			float3 clipPos;
		};
		static const fixed4 white = fixed4(1, 1, 1, 1);

		float4 _FadeCenter;
		half _FadeDistance;
		half _InnerRadius;
		half _WorldScale;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.clipPos = mul(unity_ObjectToWorld, v.vertex);
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Clip if position is outside of clip bounds
			float3 diff = abs(IN.clipPos - _FadeCenter);

			o.Albedo = _Color.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			//o.Albedo.r = clamp((_FadeDistance - length(diff) / _FadeDistance), 0, 1);
			half dist = _FadeDistance * _WorldScale;
			o.Alpha = _Color.a *clamp((_InnerRadius + (dist - length(diff)) / dist), 0, 1);
		}
		ENDCG
	}
}
