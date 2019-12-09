Shader "EditorXR/UI/Angled Vertical Gradient UI Clip"
{
    Properties
    {
        _ColorTop("Top Color", Color) = (1, 1, 1, 1)
        _ColorBottom("Bottom Color", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _AdditiveColor("Additive Color", Color) = (0.5, 0.5, 0.5, 1) // Support enabled/disabled state accents (lighter enabled, darker disabled)
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0

        // Unused _MainTex property, added to prevent runtime exceptions for elements whose parent is a ScrollRect
        [HideInInspector] _MainTex("Main Texture - unused", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Overlay+5104" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
        ZWrite Off
        ZTest Less
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha
        Fog { Mode Off }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _ColorTop;
            half4 _ColorBottom;
            half4 _AdditiveColor;
            half _Alpha;

            struct v2f
            {
                float4 position : SV_POSITION;
                half4 color : COLOR;
            };

            v2f vert(appdata_full v)
            {
                v2f output;
                output.position = UnityObjectToClipPos(v.vertex);
                // Custom-weighted upper-left to lower-right angled lerp the blends between the two gradient colors.
                // Add bias to the upper-left color.
                half lerpAmount = clamp((v.texcoord.y + 0.5) * (1.35 - v.texcoord.x), 0, 1);
                output.color = lerp(_ColorBottom, _ColorTop, lerpAmount);
                return output;
            }

            half4 frag(v2f i) : COLOR
            {
                half4 col = i.color;
                col = col + _AdditiveColor;
                col.a = _Alpha;
                return col;
            }
            ENDCG
        }
    }
}
