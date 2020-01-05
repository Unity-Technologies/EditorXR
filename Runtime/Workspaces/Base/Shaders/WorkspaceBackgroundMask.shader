Shader "EditorXR/Workspaces/WorkspaceBackgroundMask"
{
	SubShader
	{
		Tags{ "ForceNoShadowCasting" = "True" "LightMode" = "Always" "Queue" = "Transparent-1" "RenderType" = "TransparentCutout" }
		ZWrite On
		ColorMask 0
		Pass {}
	}
}
