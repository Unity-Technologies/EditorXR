Shader "EditorVR/UI/GradientButtonMask"
{
	Properties
	{
		_StencilRef("StencilRef", Int) = 3
	}

	SubShader
	{
		Tags { "Queue"="Overlay+5100" "LightMode" = "Always" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite Off
		ZTest LEqual
		ColorMask 0

		Stencil
		{
			Ref[_StencilRef]
			Pass replace
		}

		Pass {}
	}
}