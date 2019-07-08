Shader "EditorVR/RadialMenu/RadialMenuSlotMask"
{
    Properties
	{
		_StencilRef("StencilRef", Int) = 3
	}

	SubShader
	{
		Tags { "Queue"="Overlay+5500" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite Off
		ZTest LEqual
		ColorMask 0

		Stencil
		{
			Ref [_StencilRef]
			Comp always
			Pass replace
		}

		Pass {}
	}
}