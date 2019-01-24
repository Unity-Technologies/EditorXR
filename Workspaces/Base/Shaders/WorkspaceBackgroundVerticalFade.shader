Shader "EditorVR/Workspaces/WorkspaceBackgroundVerticalFade"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _Blur("Blur", Range(0, 10)) = 1
        _VerticalOffset("Offset", Range(-1, 1)) = 1
        _MainTex("Texture", 2D) = "white" {}
        _Alpha("Alpha", Range(0, 1)) = 1
        _StencilRef("StencilRef", Int) = 3
        [Toggle] _StencilFailZero("Stencil Fail Zero", Float) = 0
    }

        Category
        {
            Tags{ "Queue" = "Overlay+5102" "LightMode" = "Always" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            ZWrite On
            ZTest LEqual
            Lighting Off
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref [_StencilRef]
                Comp NotEqual
                Pass Zero
                Fail [_StencilFailZero]
            }

            SubShader
            {
                GrabPass {}

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
                        #define GrabAndOffset(weight,kernelX) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(float4(input.grab.x + _GrabTexture_TexelSize.x * kernelX * (_Blur * input.yPos), input.grab.y, input.grab.z, input.grab.w))) * weight

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

                float4 _MainTex_ST;
                fixed4 _Color;
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                sampler2D _MainTex;
                half _Alpha;
                float _WorldScale;

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
                    return col * tint;
                }
            ENDCG
            }
        }
    }
}
