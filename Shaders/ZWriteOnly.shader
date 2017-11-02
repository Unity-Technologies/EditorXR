Shader "EditorVR/Utilities/ZWriteOnly"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-1"
            "LightMode" = "Always"
            "IgnoreProjector" = "True"
        }


        Pass
        {
            ZWrite On
            ColorMask 0
        }
    }
}
