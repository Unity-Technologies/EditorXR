Shader "Custom/List Clip Transparent" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_ClipExtents("Clip Extents", Vector) = (0,0,0,0)
	}
		SubShader{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent+2" }
		LOD 200
		ZWrite Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard alpha:fade vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _MainTex;

	struct Input {
		float2 uv_MainTex;
		float3 localPos;
	};

	float4x4 _ParentMatrix;
	float4 _ClipExtents;
	half _Glossiness;
	half _Metallic;
	fixed4 _Color;

	void vert(inout appdata_full v, out Input o) {
		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.localPos = mul(_ParentMatrix, mul(UNITY_MATRIX_M, v.vertex)).xyz;
	}

	void surf(Input IN, inout SurfaceOutputStandard o) {
		// Clip if position is outside of clip bounds
		float3 diff = abs(IN.localPos);
		if (diff.x > _ClipExtents.x || diff.y > _ClipExtents.y || diff.z > _ClipExtents.z)
			discard;

		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) *_Color;
		o.Emission = c.rgb;
		o.Alpha = c.a;
	}
	ENDCG
	}
		FallBack "Diffuse"
}