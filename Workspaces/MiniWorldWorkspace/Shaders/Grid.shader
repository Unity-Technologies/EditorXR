Shader "Grid" 
{
	Properties
	{
		_GridThickness("Grid Thickness", Float) = 0.5
		_GridSpacing("Grid Spacing", Float) = (1.0, 1.0, 1.0)
		_GridCenter("Grid Center", Float) = (0.0, 0.0, 0.0)
		_GridScale("Grid Scale", Float) = 1.0
		_GridFade("Grid Fade", Float) = 1.0
		_GridColour("Grid Colour", Color) = (0.5, 1.0, 1.0, 1.0)
		_Subdivisions("Subdivisions", Float) = 8
		_SubdivisionTransparency("Subdivision Transparency", Float) = 0.5
		_ClipExtents("Clip Extents", Float) = (1.0, 1.0, 1.0, 1.0)
	}

	SubShader
	{
		Tags{ "ForceSupported" = "True" "Queue" = "Overlay+5105" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

		Pass
		{
			Blend One OneMinusSrcAlpha
			ColorMask RGB
			Cull Off
			Lighting Off
			ZWrite Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				uniform float _GridThickness;
				uniform float2 _GridSpacing;
				uniform float4 _GridCenter;
				uniform float _GridScale;
				uniform float _GridFade;
				uniform float4 _GridColour;
				uniform float _Subdivisions;
				uniform float _SubdivisionTransparency;
				uniform float4 _ClipExtents;
				uniform float4 _ClipCenter;
				uniform float4x4 _InverseRotation;

				struct vertexInput
				{
					float4 vertex : POSITION;
				};

				struct vertexOutput
				{
					float4 pos : SV_POSITION;
					float4 objectPos : TEXCOORD0;
					float4 clipPos : TEXCOORD1;
				};

				vertexOutput vert(vertexInput input) 
				{
					vertexOutput output;
					output.pos			= UnityObjectToClipPos(input.vertex);
					output.objectPos	= input.vertex - _GridCenter;
					output.clipPos = mul(_InverseRotation, mul(unity_ObjectToWorld, input.vertex));
					return output;
				}

				float lineSamplerLevel0(float2 pp, float2 lineSize)
				{
					lineSize /= _GridSpacing;
					float2 t = abs((frac(pp + (lineSize * 0.5)) * (2 / lineSize)) - 1);
					float2 s = saturate(exp(-1 * pow(t, 80)));
					return max(s.x, s.y);
				}

				float lineSamplerLevel1(float2 pp, float2 lineSize, float2 stepSize)
				{
					return max(lineSamplerLevel0(pp * _Subdivisions / stepSize, lineSize.y * _Subdivisions) * _SubdivisionTransparency,
							   lineSamplerLevel0(pp                 / stepSize, lineSize.x                ));
				}

				float lineSampler(float2 pp, float2 lineSize, float2 stepSize, float gridDepthFade)
				{
					return lerp(lineSamplerLevel1(pp, lineSize, stepSize * 2),
								lineSamplerLevel1(pp, lineSize, stepSize),
						pow(gridDepthFade, 3) * (gridDepthFade * (6 * gridDepthFade - 15) + 10));
				}

				float4 frag(vertexOutput input) : COLOR
				{
					float3 diff = abs(input.clipPos - _ClipCenter);
					if (diff.x > _ClipExtents.x || diff.y > _ClipExtents.y || diff.z > _ClipExtents.z)
						discard;

					float	gridDepth = log2(_GridFade);
					float	gridDepthFloor		= floor(gridDepth);
					float	gridDepthFade		= 1 - (gridDepth - gridDepthFloor);
					float2	stepSize			= _GridSpacing * pow(2, gridDepthFloor);

					float	lineSizeIncrease	= max(1, (pow(2, gridDepth - gridDepth)));
					float2	lineSize			= float2(0.5 * _GridThickness / lineSizeIncrease, 0.25* _GridThickness / lineSizeIncrease);
					float2  uv	=	(input.objectPos.xy / input.objectPos.w);

					uv.x *= _GridScale;
					uv.y *= _GridScale;
#if SHADER_TARGET < 25
					float	p = lineSampler(uv, lineSize, stepSize, gridDepthFade);
#else
					float2	pixelSize = fwidth(uv); // TODO: find ddx/ddy alternative or make this work in editor on older shader models some other way
					float	p	=	lineSampler(uv + (float2( 0.125, -0.375) * pixelSize), lineSize, stepSize, gridDepthFade) * 0.25
								+	lineSampler(uv + (float2(-0.375, -0.125) * pixelSize), lineSize, stepSize, gridDepthFade) * 0.25
								+	lineSampler(uv + (float2( 0.375,  0.125) * pixelSize), lineSize, stepSize, gridDepthFade) * 0.25
								+	lineSampler(uv + (float2(-0.125,  0.375) * pixelSize), lineSize, stepSize, gridDepthFade) * 0.25;
#endif

					float4	color = _GridColour;
					color.a *= p;
					color.rgb	*= color.a;

					return color;
				}
			ENDCG
		}
	}
}