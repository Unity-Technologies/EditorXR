Shader "EditorVR/SpatialUI/BackgroundOverlayBlur"
{
    Properties
    {
        _Blur("Blur", Range(0, 10)) = 1
        _VerticalOffset("Offset", Range(-1, 1)) = 1
        _Alpha("Alpha", Range(0, 1)) = 1
        //_StencilRef("StencilRef", Int) = 3
        _GradientSize("Gradient Size", Range(0, 6)) = 2
        _MainTex("Noise Texture (REQUIRED for Blur Noise)", 2D) = "white" {}
        _BlurNoise("Blur Noise Amount", Range(-1, 1)) = 1
        //[Toggle] _StencilFailZero("Stencil Fail Zero", Float) = 0
    }

        Category
        {
            Tags{ "Queue" = "Overlay+5600" "LightMode" = "Always" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            ZWrite On
            ZTest LEqual
            Lighting Off
            Blend SrcAlpha OneMinusSrcAlpha

            //Stencil
            //{
                //Ref [_StencilRef]
                //Comp NotEqual
                //Pass Zero
                //Fail [_StencilFailZero]
            //}

            SubShader
            {
                GrabPass {}

                Pass
                {
                    Name "SpatialUIBlurHorizontal"
                    CGPROGRAM

                    #pragma vertex vert
                    #pragma fragment frag
                    //#pragma fragmentoption ARB_precision_hint_fastest
                    #include "UnityCG.cginc"

                    struct appdata_t
                    {
                        float4 position : POSITION;
                        float2 texcoord : TEXCOORD0;
                    };

                    struct v2f
                    {
                        float4 position : POSITION;
                        float4 grab : TEXCOORD0;
                        float yPos : FLOAT;
                        float2 cleanUV : TEXCOORD2;
                    };

                    sampler2D _GrabTexture;
                    float4 _GrabTexture_TexelSize;
                    float _Blur;
                    float _VerticalOffset;
                    float _WorldScale;
                    half _GradientSize;
                    sampler2D _MainTex;
                    half _BlurNoise;

                    v2f vert(appdata_t v)
                    {
                        v2f output;
                        output.position = UnityObjectToClipPos(v.position);
#if UNITY_UV_STARTS_AT_TOP
                        float sign = -1.0;
                        output.yPos = v.texcoord.y;
#else
                        float sign = 1.0;
                        output.yPos = -v.texcoord.y;
#endif
                        output.grab.xy = (float2(output.position.x, output.position.y * sign) + output.position.w) * 0.5;
                        output.grab.zw = output.position.zw;
                        output.grab *= _WorldScale;

                        output.cleanUV = v.texcoord;// float2(v.texcoord.x, v.texcoord.y * sign);// float2(output.position.x, output.position.y * sign);
                        return output;
                    }

                    half4 frag(v2f input) : COLOR
                    {
                        float uvPos = length(input.cleanUV - float2(0.5, 0.5));// * 1.41421356237;
                        float xAdjustedPosition = pow(input.cleanUV.x * (1 - input.cleanUV.x) * 3, 1);
                        float yAdjustedPosition = (1 - input.cleanUV.y);
                        half positionAdjustedBlur = _Blur * yAdjustedPosition * xAdjustedPosition;

                        float4 sum = half4(0,0,0,0);
                        #define GrabAndOffset(weight,kernelX) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(float4(input.grab.x + _GrabTexture_TexelSize.x * kernelX * (positionAdjustedBlur * input.yPos), input.grab.y, input.grab.z, input.grab.w))) * weight

                        half4 color = tex2D(_MainTex, input.cleanUV);
                        //half noiseSampledTextureAmount = 0.5 + color.r - 0.00125;
                        half noiseSampledTextureAmount = 1 + color.r - 0.125 * dot(input.cleanUV, float2(0.5, 0.5));

                        half blurAdjustmentModifier = _BlurNoise * 0.25;
                        float adjustedBlur = 1;// - uvPos * 2;// * (input.cleanUV.y);
                        half adjustedBlurKernel = input.cleanUV.y;
                        sum += GrabAndOffset(0.02 * adjustedBlur, -6.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.04 * adjustedBlur, -5.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.06 * adjustedBlur, -4.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.08 * adjustedBlur, -3.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.10 * adjustedBlur, -2.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.12 * adjustedBlur, -1.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.14 * adjustedBlur, 0.0);
                        sum += GrabAndOffset(0.12 * adjustedBlur, 1.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.10 * adjustedBlur, 2.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.08 * adjustedBlur, 3.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.06 * adjustedBlur, 4.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.04 * adjustedBlur, 5.0 * noiseSampledTextureAmount);
                        noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                        sum += GrabAndOffset(0.02 * adjustedBlur, +6.0 * noiseSampledTextureAmount);


                        //float t = length(float2(0.5, 0.5) - input.grab.xy);// * 1.41421356237;
                        //sum.a *= t * lerp(0, 1, t + (_GradientSize - 0.5) * 2);
                        
                        //float t = length(input.cleanUV - float2(0.5, 0.5));// * 1.41421356237;
                        //sum.a = lerp(0, 1, uvPos - 0.5);// t  - pow(t, 10));

                        //sum.a = clamp(0, 1, 1 - (uvPos * 2));
                        float fadeFromBorderAmount = 1 - clamp(0, 1, pow(uvPos, _GradientSize) * 2);
                        //sum.a = 0;// fadeFromBorderAmount;
                        sum.a = clamp(0, 1 - pow((uvPos * 2), _GradientSize * (_Blur / 10)), fadeFromBorderAmount);

                        return sum;
                    }
                    ENDCG
                }

            GrabPass{}

            Pass
            {
                Name "SpatialUIBlurVertical"
                
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                //#pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 position : POSITION;
                    float2 texcoord: TEXCOORD0;
                };

                struct v2f
                {
                    float4 position : POSITION;
                    float4 grab : TEXCOORD0;
                    float yPos : FLOAT;
                    float2 cleanUV : TEXCOORD2;
                };

                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Blur;
                float _VerticalOffset;
                float _WorldScale;
                half _GradientSize;
                sampler2D _MainTex;
                half _BlurNoise;

                v2f vert(appdata_t v)
                {
                    v2f output;
                    output.position = UnityObjectToClipPos(v.position);
#if UNITY_UV_STARTS_AT_TOP
                    float sign = -1.0;
                    output.yPos = v.texcoord.y;
#else
                    float sign = 1.0;
                    output.yPos = -v.texcoord.y;
#endif
                    output.grab.xy = (float2(output.position.x, output.position.y * sign) + output.position.w) * 0.5;
                    output.grab.zw = output.position.zw;
                    output.grab *= _WorldScale;

                    output.cleanUV = v.texcoord;
                    return output;
                }

                half4 frag(v2f input) : COLOR
                {
                    float uvPos = length(input.cleanUV - float2(0.5, 0.5));// * 1.41421356237;
                    float xAdjustedPosition = pow(input.cleanUV.x * (1 - input.cleanUV.x) * 3, 1);
                    float yAdjustedPosition = (1 - input.cleanUV.y);
                    half positionAdjustedBlur = _Blur * yAdjustedPosition * xAdjustedPosition;

                    half4 sum = half4(0,0,0,0);
                    #define GrabAndOffset(weight,kernelY) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(input.grab.x, input.grab.y + _GrabTexture_TexelSize.y * kernelY * (positionAdjustedBlur * input.yPos + _VerticalOffset), input.grab.z, input.grab.w))) * weight

                    half4 color = tex2D(_MainTex, input.cleanUV);
                    half noiseSampledTextureAmount = 1 + color.g - 0.125 * dot(input.cleanUV, float2(0.5, 0.5));

                    float adjustedBlur = 1;// - uvPos * 2;// * (input.cleanUV.y);
                    half blurAdjustmentModifier = _BlurNoise * 0.25;
                    half adjustedBlurKernel = input.cleanUV.y;
                    sum += GrabAndOffset(0.02 * adjustedBlur, -6.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.04 * adjustedBlur, -5.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.06 * adjustedBlur, -4.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.08 * adjustedBlur, -3.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.10 * adjustedBlur, -2.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.12 * adjustedBlur, -1.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.14 * adjustedBlur, 0.0);
                    sum += GrabAndOffset(0.12 * adjustedBlur, +1.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.10 * adjustedBlur, +2.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.08 * adjustedBlur, +3.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.06 * adjustedBlur, +4.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount -= noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.04 * adjustedBlur, +5.0 * noiseSampledTextureAmount);
                    noiseSampledTextureAmount += noiseSampledTextureAmount * blurAdjustmentModifier;
                    sum += GrabAndOffset(0.02 * adjustedBlur, +6.0 * noiseSampledTextureAmount);


                    //float t = length(input.grab - float2(0.5, 0.5)) * 1.41421356237;
                    //sum.a *= t * lerp(0, 1, t + (_GradientSize - 0.5) * 2);

                    //sum.a = 0;
                    //float fadeFromBorderAmount = 1 - clamp(0, 1, pow(uvPos, _GradientSize) * 2);
                    float fadeFromBorderAmount = 1 - clamp(0, 1, pow(uvPos, _GradientSize) * 2);// - pow((uvPos * 2), _GradientSize);
                    //sum.a = 0;// clamp(0, 1 - pow((uvPos * 2), _GradientSize * (_Blur / 10)), fadeFromBorderAmount);
                    sum.a = clamp(0, 1 - pow((uvPos * 2), _GradientSize * (_Blur / 10)), fadeFromBorderAmount);

                    return sum;
                }
                ENDCG
            }

            GrabPass{}

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 position : POSITION;
                    float2 texcoord: TEXCOORD0;
                };

                struct v2f
                {
                    float4 position : POSITION;
                    float4 grab : TEXCOORD0;
                    float2 uvmain : TEXCOORD2;
                };

                float4 _MainTex_ST;
                fixed4 _Color;
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                sampler2D _MainTex;
                half _Alpha;
                float _WorldScale;
                half _GradientSize;

                v2f vert(appdata_t v)
                {
                    v2f output;
                    output.position = UnityObjectToClipPos(v.position);
#if UNITY_UV_STARTS_AT_TOP
                    float sign = -1.0;
#else
                    float sign = 1.0;
#endif
                    output.grab.xy = (float2(output.position.x, output.position.y * sign) + output.position.w) * 0.5;
                    output.grab.zw = output.position.zw;
                    output.grab *= _WorldScale;
                    output.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);
                    return output;
                }

                half4 frag(v2f i) : COLOR
                {
                    i.grab.xy = _GrabTexture_TexelSize.xy * i.grab.z + i.grab.xy;
                    half4 col = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.grab));
                    half4 desatCol = dot(col, col);
                    col = lerp(col * col, desatCol, 0.2);
                    half4 tint = tex2D(_MainTex, i.uvmain) * _Color;

                    col.a = _Alpha;
                    tint.a = _Alpha;

                    float t = length(i.grab - float2(0.5, 0.5)) * 1.41421356237;
                    half4 combinedColor = col * tint;
                    combinedColor.a *= t * lerp(0, 1, t + (_GradientSize - 0.5) * 2);;

                    combinedColor.a = 0;// _Color.a;

                    return combinedColor;
                }
            ENDCG
            }
        }
    }
}
