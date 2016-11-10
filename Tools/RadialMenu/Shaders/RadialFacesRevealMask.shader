Shader "EditorVR/RadialMenu/RadialFacesRevealMask"
{
	SubShader
	{
		Tags { "Queue"="Geometry+1" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite Off
		ColorMask 0
		
		Stencil
		{
			Ref 1
			Comp always
			Pass DecrSat
		}

		Pass {}
	}
}