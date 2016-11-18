Shader "EditorVR/TransparentBlur"
{
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_Blur("Blur", Range(0, 10)) = 1
		}

		Category
		{
			Tags{ "Queue" = "Geometry" "LightMode" = "Always" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
			ZWrite On

			SubShader{

				GrabPass{}

				Pass{
					CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma fragmentoption ARB_precision_hint_fastest
					#include "UnityCG.cginc"

					struct appdata_t {
						float4 position : POSITION;
						float2 texcoord: TEXCOORD0;
					};

					struct v2f {
						float4 position : POSITION;
						float4 grab : TEXCOORD0;
					};

					v2f vert(appdata_t v) {
						v2f output;
						output.position = mul(UNITY_MATRIX_MVP, v.position);
#if UNITY_UV_STARTS_AT_TOP
						float scale = -1.0;
#else
						float scale = 1.0;
#endif
						output.grab.xy = (float2(output.position.x, output.position.y*scale) + output.position.w) * 0.5;
						output.grab.zw = output.position.zw;
						return output;
					}

					sampler2D _GrabTexture;
					float4 _GrabTexture_TexelSize;
					float _Blur;

					half4 frag(v2f input) : COLOR{
						half4 sum = half4(0,0,0,0);
						#define GrabAndOffset(weight,kernelx) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(input.grab.x + _GrabTexture_TexelSize.x * kernelx * _Blur * 1.25, input.grab.y, input.grab.z, input.grab.w))) * weight
						
						sum += GrabAndOffset(0.02, -5.0);
						sum += GrabAndOffset(0.04, -4.0);
						sum += GrabAndOffset(0.08, -3.0);
						sum += GrabAndOffset(0.11, -2.0);
						sum += GrabAndOffset(0.16, -1.0);
						sum += GrabAndOffset(0.18, 0.0);
						sum += GrabAndOffset(0.16, +1.0);
						sum += GrabAndOffset(0.11, +2.0);
						sum += GrabAndOffset(0.08, +3.0);
						sum += GrabAndOffset(0.04, +4.0);
						sum += GrabAndOffset(0.02, +5.0);
						return sum;
					}
					ENDCG
				}

			GrabPass{}

			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"

				struct appdata_t {
					float4 position : POSITION;
					float2 texcoord: TEXCOORD0;
				};

				struct v2f {
					float4 position : POSITION;
					float4 grab : TEXCOORD0;
				};

				v2f vert(appdata_t v) {
					v2f output;
					output.position = mul(UNITY_MATRIX_MVP, v.position);
	#if UNITY_UV_STARTS_AT_TOP
					float scale = -1.0;
	#else
					float scale = 1.0;
	#endif
					output.grab.xy = (float2(output.position.x, output.position.y*scale) + output.position.w) * 0.5;
					output.grab.zw = output.position.zw;
					return output;
				}

				sampler2D _GrabTexture;
				float4 _GrabTexture_TexelSize;
				float _Blur;

				half4 frag(v2f input) : COLOR{
					half4 sum = half4(0,0,0,0);
					#define GrabAndOffset(weight,kernely) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(input.grab.x, input.grab.y + _GrabTexture_TexelSize.y * kernely*_Blur, input.grab.z, input.grab.w))) * weight

					sum += GrabAndOffset(0.02, -5.0);
					sum += GrabAndOffset(0.04, -4.0);
					sum += GrabAndOffset(0.08, -3.0);
					sum += GrabAndOffset(0.11, -2.0);
					sum += GrabAndOffset(0.16, -1.0);
					sum += GrabAndOffset(0.18,  0.0);
					sum += GrabAndOffset(0.16, +1.0);
					sum += GrabAndOffset(0.11, +2.0);
					sum += GrabAndOffset(0.08, +3.0);
					sum += GrabAndOffset(0.04, +4.0);
					sum += GrabAndOffset(0.02, +5.0);
					return sum;
				}
				ENDCG
			}

			GrabPass{}

			Pass{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"

				struct appdata_t {
					float4 position : POSITION;
					float2 texcoord: TEXCOORD0;
				};

				struct v2f {
					float4 position : POSITION;
					float4 grab : TEXCOORD0;
					float2 uvmain : TEXCOORD2;
				};
				float4 _MainTex_ST;

				v2f vert(appdata_t v) {
					v2f output;
					output.position = mul(UNITY_MATRIX_MVP, v.position);
	#if UNITY_UV_STARTS_AT_TOP
					float scale = -1.0;
	#else
					float scale = 1.0;
	#endif
					output.grab.xy = (float2(output.position.x, output.position.y*scale) + output.position.w) * 0.5;
					output.grab.zw = output.position.zw;
					output.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);
					return output;
				}

				fixed4 _Color;
				sampler2D _GrabTexture;
				float4 _GrabTexture_TexelSize;
				sampler2D _MainTex;

				half4 frag(v2f i) : COLOR{
					i.grab.xy = _GrabTexture_TexelSize.xy * i.grab.z + i.grab.xy;
					half4 col = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.grab));
					half4 desatCol = dot(col, col);
					col = lerp(col * col, desatCol, 0.2);
					half4 tint = tex2D(_MainTex, i.uvmain) * _Color;

					return col * tint;
				}
			ENDCG
			}
		}
	}
}