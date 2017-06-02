// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "EditorVR/Annotation/Line"
 {
	
    Properties
	{
        _Texture ("Texture", 2D) = "white" {}
        _EmissionColor ("Emission Color", Color) = (0.5,0.5,0.5,1)
    }
	
    SubShader
	{
		
        Tags
		{
            "IgnoreProjector"="True"
            "Queue"="AlphaTest"
            "RenderType"="Transparent"
        }
		
        Pass
		{
            Name "FORWARD"
			
            Tags
			{
                "LightMode"="ForwardBase"
            }
			
			AlphaToMask On
            Cull Off
            
            CGPROGRAM
			
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
			
            uniform float4 _EmissionColor;
            uniform sampler2D _Texture;
			uniform float4 _Texture_ST;
			
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
                float4 tex = tex2D(_Texture, TRANSFORM_TEX(i.uv0, _Texture));
                float3 finalColor = _EmissionColor.rgb * tex.rgb;				
                fixed4 finalRGBA = fixed4(finalColor, tex.a);
                return finalRGBA;
            }
			
            ENDCG
        }
    }
	
    FallBack "Diffuse"
}
