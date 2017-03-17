Shader "EditorVR/ProxyGradientOutline"
{
	Properties
	{
		_ColorTop ("Top Color", Color) = (1,1,1,1)
		_ColorBottom ("Bottom Color", Color) = (1,1,1,1)
		_Alpha ("Alpha", Range(0, 1)) = 1
		_Thickness ("Thickness", Range (0, 0.25)) = 0.02
		_StencilRef ("StencilRef", Int) = 0
		_ObjectScale ("Object Scale", Range (-0.1, 0.1)) = -0.05
		[Toggle(ROTATE_GRADIENT)] _RotateGradient("Rotate Gradient", Float) = 0
	}

	SubShader
	{
		Tags{ "Queue" = "Geometry" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "LightMode" = "Always"}

		// Write stencil
		Pass
		{
			ZTest Off
			ZWrite Off
			Cull Back
			ColorMask 0

			Stencil
			{
				Ref [_StencilRef]
				Pass Replace
			}

			CGPROGRAM

			#include "UnityCG.cginc"
			#pragma vertex vertStencil
			#pragma fragment fragStencil

			struct v2f
			{
				float4 position : SV_POSITION;
			};

			v2f vertStencil(appdata_full v)
			{
				v2f output;
				output.position = UnityObjectToClipPos(v.vertex);
				return output;
			}

			float4 fragStencil(v2f input) : SV_Target
			{
				return 0;
			}

			ENDCG
		}

		// Draw outline
		Pass
		{
			ZTest Off
			ZWrite On
			Cull Back
			Blend SrcAlpha OneMinusSrcAlpha

			Stencil
			{
				Ref [_StencilRef]
				Comp NotEqual
			}

			CGPROGRAM

			#include "UnityCG.cginc"
			#pragma vertex vertOutline
			#pragma fragment fragOutline
			#pragma multi_compile __ ROTATE_GRADIENT

			fixed4 _ColorTop;
			fixed4 _ColorBottom;
			half _Thickness;
			half _ObjectScale;
			half _Alpha;

			struct v2f
			{
				float4 position : SV_POSITION;
				fixed4 color : COLOR;
				half3 localPosition : FLOAT;
			};

			v2f vertOutline(appdata_full v)
			{
				v2f output;

				output.position = UnityObjectToClipPos(v.vertex);
				output.color = lerp(_ColorBottom, _ColorTop, v.vertex);
				output.localPosition = v.vertex.xyz;

				float3 norm = normalize( mul((float3x3)UNITY_MATRIX_IT_MV, v.normal) );
				float2 offset = TransformViewToProjection(norm.xy);
				output.position.xy += offset * _Thickness * 0.1;
				return output;
			}

			float4 fragOutline(v2f input) : SV_Target
			{
				#ifdef ROTATE_GRADIENT
				half localPos = input.localPosition.y;
				#else
				half localPos = input.localPosition.z;
				#endif

				half4 color = lerp(_ColorTop, _ColorBottom, localPos / _ObjectScale);
				color.a = _Alpha;

				return color;
			}

			ENDCG
		}
	}
}
