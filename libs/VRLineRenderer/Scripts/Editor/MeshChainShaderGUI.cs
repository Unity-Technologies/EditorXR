using UnityEngine;

namespace UnityEditor
{
public class MeshChainShaderGUI : ShaderGUI
{
	protected MaterialEditor m_MaterialEditor;
	protected bool m_FirstTimeApply = true;

	protected MaterialProperty m_LineColor = null;
	protected MaterialProperty m_LineSettings = null;
	protected MaterialProperty m_LineRadius = null;

	protected MaterialProperty m_LineDataSpace = null;
	protected MaterialProperty m_LineDepthScaleMode = null;

	protected static class Styles
	{
		public static string emptyTootip = "";
		public static GUIContent colorText = new GUIContent("Line Tint", "Line Color (RGB) and Transparency (A)");
		public static GUIContent lineDataSpaceText = new GUIContent("World Space Data", "If true, the data will not be transformed before rendering");

		public static string lineSettingsText = "Line Rendering Levels";
		public static GUIContent lineCurveMinText = new GUIContent("Minimum Cutoff", "How far from the edge of the geometry to start fading in line");
		public static GUIContent lineCurveMaxText = new GUIContent("Maximum Cutoff", "How far from the center of the geometry to start fading out the line");
		public static GUIContent lineCurveBendText = new GUIContent("Level Curve", "The intensity curve from line edge to line center");

		public static string lineRadiusText = "Line Radius Settings";
		public static GUIContent lineRadiusDepthText = new GUIContent("Line Scaled by Depth", "Set to false to get fixed width lines - useful for debugging or drafting views");
		public static GUIContent lineRadiusScaleText = new GUIContent("Radius Scale", "How much to scale the width of the line in total");
		public static GUIContent lineRadiusMinText = new GUIContent("Radius Minimum", "Minimum size the line width must be regardless of distance");
		public static GUIContent lineRadiusMaxText = new GUIContent("Radius Maximum", "Maximum size the line widht must be regardless of distance");
	}

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
	{
		FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
		m_MaterialEditor = materialEditor;
		Material material = materialEditor.target as Material;

		ShaderPropertiesGUI(material);

		// Make sure that needed setup (ie keywords/renderqueue) are set up
		if (m_FirstTimeApply)
		{
			MaterialChanged(material);
			m_FirstTimeApply = false;
		}
	}

	public void FindProperties(MaterialProperty[] props)
	{
		m_LineColor = FindProperty("_Color", props);
		m_LineSettings = FindProperty("_lineSettings", props);
		m_LineRadius = FindProperty("_lineRadius", props);

		// CCS Customizations
		m_LineDataSpace = FindProperty("_WorldData", props, false);
		m_LineDepthScaleMode = FindProperty("_LineDepthScale", props, false);
	}

	public void ShaderPropertiesGUI(Material material)
	{
		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
			// Color
			m_MaterialEditor.ShaderProperty(m_LineColor, Styles.colorText.text);

			EditorGUILayout.Space();

			// World space flag
			if (m_LineDataSpace != null)
			{
				bool inWorldSpace = (m_LineDataSpace.floatValue != 0.0f);
				var newWorldSpace = EditorGUILayout.Toggle(Styles.lineDataSpaceText, inWorldSpace);
				if (newWorldSpace != inWorldSpace)
				{
					if (newWorldSpace == true)
					{
						m_LineDataSpace.floatValue = 1.0f;
					}
					else
					{
						m_LineDataSpace.floatValue = 0.0f;
					}
				}
			}
				

			EditorGUILayout.Space();
			// Line thickness curve settings
			GUILayout.Label(Styles.lineSettingsText, EditorStyles.boldLabel);
			var lineLevels = m_LineSettings.vectorValue;
			var newLineMin = EditorGUILayout.Slider(Styles.lineCurveMinText, lineLevels.x, 0.0f, 1.0f);
			var newLineMax = EditorGUILayout.Slider(Styles.lineCurveMaxText, lineLevels.y, 0.0f, 1.0f);
			var newLineBend = EditorGUILayout.Slider(Styles.lineCurveBendText, lineLevels.z, 0.0f, 1.0f);

			if (newLineMin != lineLevels.x || newLineMax != lineLevels.y || newLineBend != lineLevels.z)
			{
				lineLevels.x = newLineMin;
				lineLevels.y = newLineMax;
				lineLevels.z = newLineBend;
				m_LineSettings.vectorValue = lineLevels;
			}
				
			EditorGUILayout.Space();
			// Maximum line radius
			GUILayout.Label(Styles.lineRadiusText, EditorStyles.boldLabel);
			var radiusSettings = m_LineRadius.vectorValue;
			var depthScaleMode = true;
			if (m_LineDepthScaleMode != null)
			{
				depthScaleMode = (m_LineDepthScaleMode.floatValue != 0.0f);
				var newDepthScaleMode = EditorGUILayout.Toggle(Styles.lineRadiusDepthText, depthScaleMode);
				if (newDepthScaleMode != depthScaleMode)
				{
					if (newDepthScaleMode == true)
					{
						m_LineDepthScaleMode.floatValue = 1.0f;
					}
					else
					{
						m_LineDepthScaleMode.floatValue = 0.0f;
					}
				}
			}
			var newRadiusScale = EditorGUILayout.FloatField(Styles.lineRadiusScaleText, radiusSettings.x);
			var newRadiusMin = radiusSettings.y;
			var newRadiusMax = radiusSettings.z;

			if (depthScaleMode == false)
			{
				newRadiusMin =  Mathf.Max(EditorGUILayout.FloatField(Styles.lineRadiusMinText, radiusSettings.y), 0.0f);
				newRadiusMax =  Mathf.Max(EditorGUILayout.FloatField(Styles.lineRadiusMaxText, radiusSettings.z), newRadiusMin);
			}

			if (newRadiusScale != radiusSettings.x || newRadiusMin != radiusSettings.y || newRadiusMax != radiusSettings.z)
			{
				radiusSettings.x = newRadiusScale;
				radiusSettings.y = newRadiusMin;
				radiusSettings.z = newRadiusMax;
				m_LineRadius.vectorValue = radiusSettings;
			}
		}
		if (EditorGUI.EndChangeCheck())
		{
			MaterialChanged(material);
		}
	}

	static void MaterialChanged(Material material)
	{
		var worldDataMode = false;
		var depthScaleMode = true;
		if (material.HasProperty("_WorldData"))
		{
			worldDataMode = (material.GetFloat("_WorldData") != 0.0f);
		}
		if (material.HasProperty("_LineDepthScale"))
		{
			depthScaleMode = (material.GetFloat("_LineDepthScale") != 0.0f);
		}
		if (worldDataMode)
		{
			SetKeyword(material, "LINE_MODEL_SPACE", false);
			SetKeyword(material, "LINE_WORLD_SPACE", true);
		}
		else
		{
			SetKeyword(material, "LINE_MODEL_SPACE", true);
			SetKeyword(material, "LINE_WORLD_SPACE", false);
		}

		if (depthScaleMode)
		{
			SetKeyword(material, "LINE_FIXED_WIDTH", false);
			SetKeyword(material, "LINE_PERSPECTIVE_WIDTH", true);
		}
		else
		{
			SetKeyword(material, "LINE_FIXED_WIDTH", true);
			SetKeyword(material, "LINE_PERSPECTIVE_WIDTH", false);
		}
	}

	static void SetKeyword(Material m, string keyword, bool state)
	{
		if (state)
			m.EnableKeyword (keyword);
		else
			m.DisableKeyword (keyword);
	}
}
}
