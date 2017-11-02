// Custom shader used to write depth behind semi-transparent proxy materials, fixing transparency sorting visual issues
Shader "EditorVR/Utilities/Semitransparent Proxy Background"
{
	Properties
	{
        _Color("Main Color", Color) = (1,1,1,1)
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
 
            struct vertexInput
            {
                float4 position: POSITION;
            };
 
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
 
            v2f vert(vertexInput input)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(input.position);
                return o;
            }
 
            half4 frag(v2f input) : COLOR
            {
                return _Color;
            } 
            ENDCG
        }
    }
}
