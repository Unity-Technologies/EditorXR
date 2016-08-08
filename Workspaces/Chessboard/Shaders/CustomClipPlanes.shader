Shader "Custom/Custom Clip Planes" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_ClipCenter("Clip Center", Vector) = (0,0,0,1)
		//_ClipDistance("Clip Distance", Float) = 1.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf NoLighting noforwardadd fullforwardshadows		

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		static const uint _PlaneCount = 6;
		static const half4 _PlaneNormals[_PlaneCount] = {
			half4(-1, 0, 0, 0),
			half4(0, 0, 1, 0),
			half4(1, 0, 0, 0),
			half4(0, 0, -1, 0),
			half4(0, -1, 0, 0),
			half4(0, 1, 0, 0)
		};
		static const fixed4 white = fixed4(1, 1, 1, 1);

		float4 _ClipCenter;
		//half _ClipDistance;
		//half _ClipDistance[_PlaneCount];
		half _ClipDistance0;
		half _ClipDistance1;
		half _ClipDistance2;
		half _ClipDistance3;
		half _ClipDistance4;
		half _ClipDistance5;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) { return fixed4(0, 0, 0, 0); }

		void surf (Input IN, inout SurfaceOutput o) {
			// Clip against planes equidistant from the clip center point
			clip(dot(IN.worldPos - (float3)(_ClipCenter - _PlaneNormals[0] * _ClipDistance0), (float3)_PlaneNormals[0]));
			clip(dot(IN.worldPos - (float3)(_ClipCenter - _PlaneNormals[1] * _ClipDistance1), (float3)_PlaneNormals[1]));
			clip(dot(IN.worldPos - (float3)(_ClipCenter - _PlaneNormals[2] * _ClipDistance2), (float3)_PlaneNormals[2]));
			clip(dot(IN.worldPos - (float3)(_ClipCenter - _PlaneNormals[3] * _ClipDistance3), (float3)_PlaneNormals[3]));
			clip(dot(IN.worldPos - (float3)(_ClipCenter - _PlaneNormals[4] * _ClipDistance4), (float3)_PlaneNormals[4]));
			clip(dot(IN.worldPos - (float3)(_ClipCenter - _PlaneNormals[5] * _ClipDistance5), (float3)_PlaneNormals[5]));

			// Some materials don't have colors set, so default them to white
			if (dot(_Color, white) <= 0)
				_Color = white;

			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) *_Color;
			o.Emission = c.rgb;			
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
