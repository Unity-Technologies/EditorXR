Shader "EditorXR/Custom/Custom Clip Planes"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			//#pragma surface surf NoLighting nolightmap noforwardadd noshadow nometa vertex:vert
			#pragma surface surf Standard vertex:vert

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0
			sampler2D _MainTex;

			struct Input
			{
				float2 uv_MainTex;
				float3 clipPos;
			};
			static const fixed4 white = fixed4(1, 1, 1, 1);

			float4 _GlobalClipCenter;
			float4 _GlobalClipExtents;
			half _Glossiness;
			half _Metallic;
			fixed4 _Color;
			float4x4 _InverseRotation;

			void vert(inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				o.clipPos = mul(_InverseRotation, mul(unity_ObjectToWorld, v.vertex));
			}

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				// Clip if position is outside of clip bounds
				float3 diff = abs(IN.clipPos - _GlobalClipCenter);
				if (diff.x > _GlobalClipExtents.x || diff.y > _GlobalClipExtents.y || diff.z > _GlobalClipExtents.z)
					discard;

				// Some materials don't have colors set, so default them to white
				if (dot(_Color, white) <= 0)
					_Color = white;

				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) *_Color;
				o.Albedo = c.rgb;
				// Metallic and smoothness come from slider variables
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			}
		ENDCG
	}

	SubShader
	{
		Tags{"RenderType" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
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

			float4 _GlobalClipCenter;
			float4 _GlobalClipExtents;
			half _Glossiness;
			half _Metallic;
			fixed4 _Color;
			float4x4 _InverseRotation;

			void vert(inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				o.clipPos = mul(_InverseRotation, mul(unity_ObjectToWorld, v.vertex));
			}

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				// Clip if position is outside of clip bounds
				float3 diff = abs(IN.clipPos - _GlobalClipCenter);
				if (diff.x > _GlobalClipExtents.x || diff.y > _GlobalClipExtents.y || diff.z > _GlobalClipExtents.z)
					discard;

				// Some materials don't have colors set, so default them to white
				if (dot(_Color, white) <= 0)
					_Color = white;

				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) *_Color;
				o.Albedo = c.rgb;
				// Metallic and smoothness come from slider variables
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			}
		ENDCG
	}

	SubShader
	{
		Tags{"RenderType" = "Outline" "Queue" = "Overlay+5000"}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Render the object with stencil=1 to mask out the part that isn't the silhouette
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			Tags{"LightMode" = "Always"}
			ColorMask 0
			Cull Off
			ZWrite Off
			ZTest Off
			Stencil
			{
				Ref 1
				Comp always
				Pass replace
			}

			CGPROGRAM
				#include "Silhouette.cginc"
				#pragma vertex MainVsClip
				#pragma fragment NullPsClip
			ENDCG
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Render the outline by extruding along vertex normals and using the stencil mask previously rendered. Only render depth, so that the final pass executes
		// once per fragment (otherwise alpha blending will look bad).
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			Tags{"LightMode" = "Always"}
			Cull Off
			ZWrite Off
			ZTest Off
			Stencil
			{
				Ref 1
				Comp notequal
				Pass keep
				Fail keep
			}

			CGPROGRAM
				#include "Silhouette.cginc"
				#pragma vertex MainVsClip
				#pragma geometry ExtrudeGs
				#pragma fragment MainPsClip
			ENDCG
		}

		Pass
		{
			Tags{"LightMode" = "Always"}
			ColorMask 0
			Cull Off
			ZWrite Off
			ZTest Off
			Stencil
			{
				Ref 0
				Comp always
				Pass replace
			}

			CGPROGRAM
				#include "Silhouette.cginc"
				#pragma vertex MainVsClip
				#pragma fragment NullPsClip
			ENDCG
		}
	}
}