Shader "EditorVR/RadialMenu/RadialMenuSlotMask"
{
	SubShader
	{
		Tags { "Queue"="Overlay+5500" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite Off
		ZTest LEqual
		ColorMask 0
		
		Stencil 
		{
			Ref 1
			Comp always
			Pass replace
		}

		Pass {}
	}
}