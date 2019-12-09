Shader "EditorXR/RadialMenu/RadialFacesRevealMask"
{
	SubShader
	{
		Tags { "Queue"="Overlay+5501" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite Off
		ColorMask 0
		
		Stencil
		{
			Ref 0
			Comp Always
			Pass Replace
		}

		Pass {}
	}
}