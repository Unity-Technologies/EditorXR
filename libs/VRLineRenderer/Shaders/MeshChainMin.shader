Shader "VRLineRenderer/MeshChain - Min Color"
{
	Properties 
	{
		_Color("Color Tint", COLOR) = (1,1,1,1)
		_lineSettings ("Line Thickness Settings", VECTOR) = (0, 1, .5, 1)

		_lineRadius("Line Radius Scale, Min, Max", VECTOR) = (1, 0, 100)

		// Local space or world space data
		[HideInInspector] _WorldData("__worlddata", Float) = 0.0

		// Depth effects line width
		[HideInInspector] _LineDepthScale("__linedepthscale", Float) = 1.0
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 100

		Pass
		{
			// This version of the shader writes any pixel darker than the background
			// It uses the 'min' operation to keep line consistency
			Blend One One
			BlendOp Min
			Cull Off
			Lighting Off
			ZWrite Off
			Offset 0, -.1

			CGPROGRAM

				#pragma vertex vert
				#pragma fragment fragInvert
				#pragma	multi_compile LINE_PERSPECTIVE_WIDTH LINE_FIXED_WIDTH
				#pragma multi_compile LINE_MODEL_SPACE LINE_WORLD_SPACE

				#include "UnityCG.cginc"
				#include "MeshChain.cginc"

			ENDCG
		}
	}
	FallBack "Diffuse"
	CustomEditor "MeshChainShaderGUI"
}
