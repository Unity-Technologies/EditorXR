Shader "EditorXR/Blur/Blur Dynamic Opaque"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _Blur("Blur", Range(0, 10)) = 1
        _VerticalOffset("Offset", Range(-1, 1)) = 1
        _Alpha("Alpha", Range(0, 1)) = 1
        _Desaturation("Desaturation", Range(-1, 1)) = 0.5
        _DesaturationBlend("Desaturation Blend", Range(-1, 1)) = 0.5
    }

        Category
        {
            Tags
            {
                "Queue" = "Overlay+5102"
                "LightMode" = "Always"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
            }

            ZWrite On
            ZTest LEqual
            Lighting Off
            Blend SrcAlpha OneMinusSrcAlpha

            SubShader
            {
                // Share common initial grab pass.  Following passes need to be individual grabs
                GrabPass{ "_SharedEXRBlurDynamicOpaqueGrabBase" }

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
                        float yPos : FLOAT;
                    };

                    sampler2D _GrabTexture;
                    float4 _GrabTexture_TexelSize;
                    float _Blur;
                    float _VerticalOffset;
                    float _WorldScale;

                    v2f vert(appdata_t v)
                    {
                        v2f output;
                        UNITY_INITIALIZE_OUTPUT(v2f, output);
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
                        return output;
                    }

                    half4 frag(v2f input) : COLOR
                    {
                        half4 sum = half4(0,0,0,0);
                        #define GrabAndOffset(weight,kernelX) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(input.grab.x + _GrabTexture_TexelSize.x * kernelX * (_Blur * input.yPos), input.grab.y, input.grab.z, input.grab.w))) * weight

                        sum += GrabAndOffset(0.02, -6.0);
                        sum += GrabAndOffset(0.04, -5.0);
                        sum += GrabAndOffset(0.06, -4.0);
                        sum += GrabAndOffset(0.08, -3.0);
                        sum += GrabAndOffset(0.10, -2.0);
                        sum += GrabAndOffset(0.12, -1.0);
                        sum += GrabAndOffset(0.14, 0.0);
                        sum += GrabAndOffset(0.12, +1.0);
                        sum += GrabAndOffset(0.10, +2.0);
                        sum += GrabAndOffset(0.08, +3.0);
                        sum += GrabAndOffset(0.06, +4.0);
                        sum += GrabAndOffset(0.04, +5.0);
                        sum += GrabAndOffset(0.02, +6.0);
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
                    float yPos : FLOAT;
                };

                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Blur;
                float _VerticalOffset;
                float _WorldScale;

                v2f vert(appdata_t v)
                {
                    v2f output;
                    UNITY_INITIALIZE_OUTPUT(v2f, output);
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
                    return output;
                }

                half4 frag(v2f input) : COLOR
                {
                    half4 sum = half4(0,0,0,0);
                    #define GrabAndOffset(weight,kernelY) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(input.grab.x, input.grab.y + _GrabTexture_TexelSize.y * kernelY * (_Blur * input.yPos + _VerticalOffset), input.grab.z, input.grab.w))) * weight

                    sum += GrabAndOffset(0.02, -6.0);
                    sum += GrabAndOffset(0.04, -5.0);
                    sum += GrabAndOffset(0.06, -4.0);
                    sum += GrabAndOffset(0.08, -3.0);
                    sum += GrabAndOffset(0.10, -2.0);
                    sum += GrabAndOffset(0.12, -1.0);
                    sum += GrabAndOffset(0.14,  0.0);
                    sum += GrabAndOffset(0.12, +1.0);
                    sum += GrabAndOffset(0.10, +2.0);
                    sum += GrabAndOffset(0.08, +3.0);
                    sum += GrabAndOffset(0.06, +4.0);
                    sum += GrabAndOffset(0.04, +5.0);
                    sum += GrabAndOffset(0.02, +6.0);
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

                fixed4 _Color;
                half _Desaturation;
                half _DesaturationBlend;
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _WorldScale;

                v2f vert(appdata_t v)
                {
                    v2f output;
                    UNITY_INITIALIZE_OUTPUT(v2f, output);
                    output.position = UnityObjectToClipPos(v.position);
#if UNITY_UV_STARTS_AT_TOP
                    float scale = -1.0;
#else
                    float scale = 1.0;
#endif
                    output.grab.xy = (float2(output.position.x, output.position.y*scale) + output.position.w) * 0.5;
                    output.grab.zw = output.position.zw;
                    output.grab *= _WorldScale;
                    return output;
                }

                half4 frag(v2f i) : COLOR
                {
                    i.grab.xy = _GrabTexture_TexelSize.xy * i.grab.z + i.grab.xy;
                    half4 col = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.grab));
                    half4 desatCol = dot(col, col * _Desaturation); // Lighten darker luma
                    col = lerp(col, desatCol, _DesaturationBlend); // Blend by desaturation amount
                    col.a = _Color.a;
                    return col * _Color;
                }
                ENDCG
            }
        }
    }
}