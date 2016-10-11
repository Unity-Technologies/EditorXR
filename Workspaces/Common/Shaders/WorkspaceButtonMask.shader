Shader "EditorVR/Workspaces/WorkspaceButtonMask"
{
	SubShader
	{
		Tags { "Queue"="Geometry" "LightMode" = "Always" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
		ZWrite Off
		ZTest LEqual
		ColorMask 0

		Stencil
		{
			Ref 1
			Comp always
			Pass replace
		}

		Pass{}
	}
}