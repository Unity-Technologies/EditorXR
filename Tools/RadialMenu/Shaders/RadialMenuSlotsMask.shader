Shader "EditorVR/RadialMenu/RadialMenuSlotMask"
{
	SubShader
	{
		Tags { "Queue"="Geometry" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite On
		ZTest NotEqual
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