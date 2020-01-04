Shader "EditorXR/SpatialUI/SpatialUIBackground"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-1"
            "LightMode" = "Always"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZTest Always
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            uniform half4 _Color;
            sampler2D _MainTex;
 
            struct vertexInput
            {
                float4 position: POSITION;
                float2 texcoord: TEXCOORD0;
                float4 color : COLOR;
            };
 
            struct v2f
            {
                float4 pos : SV_POSITION;
                half2 texcoord: TEXCOORD0;
                float4 color : COLOR;
            };
 
            v2f vert(vertexInput input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.position);
                output.texcoord = input.texcoord;
                output.color = input.color;
                return output;
            }
 
            half4 frag(v2f input) : COLOR
            {
                half4 color = tex2D(_MainTex, input.texcoord) * _Color;
                color.a *= input.color.a;
                return color;
            }
            ENDCG
        }
    }
}
