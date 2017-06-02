// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "EditorVR/Annotation/Brush"
 {
	
    Properties
	{
        _EmissionColor ("Emission Color", Color) = (0.5,0.5,0.5,1)
    }
	
    SubShader
	{
		
        Tags
		{
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
		
        Pass
		{
            Name "FORWARD"
			
            Tags
			{
                "LightMode"="ForwardBase"
            }
			
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            
            CGPROGRAM
			
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
			
            uniform float4 _EmissionColor;
			
            struct VertexInput
			{
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
			
            struct VertexOutput
			{
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };
			
            VertexOutput vert (VertexInput i)
			{
                VertexOutput o = (VertexOutput)0;
                o.uv0 = i.texcoord0;
                o.pos = UnityObjectToClipPos(i.vertex);
                return o;
            }
			
            float4 frag(VertexOutput i) : COLOR
			{
                fixed4 finalRGBA = fixed4(_EmissionColor.rgb, _EmissionColor.a);
                return finalRGBA;
            }
			
            ENDCG
        }
    }
	
    FallBack "Diffuse"
}
