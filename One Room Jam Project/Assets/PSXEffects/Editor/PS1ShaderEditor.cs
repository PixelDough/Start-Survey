using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class PS1ShaderEditor : ShaderGUI {

	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
		Material material = materialEditor.target as Material;

		bool culling = Array.IndexOf(material.shaderKeywords, "BFC") != -1;

		string[] renderModes = new string[] {
			"Opaque",
			"Transparent",
			"Cutout"
		};
		float renderMode = material.GetFloat("_RenderMode");

		EditorGUI.BeginChangeCheck();
		renderMode = EditorGUILayout.Popup("Render Mode", (int)renderMode, renderModes);
		culling = EditorGUILayout.Toggle("Backface Culling", culling);
		if (EditorGUI.EndChangeCheck()) {
			material.SetFloat("_RenderMode", renderMode);
			if (renderMode == 1) {
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.EnableKeyword("TRANSPARENT");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
			} else {
				material.SetOverrideTag("RenderType", "Opaque");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.DisableKeyword("TRANSPARENT");
				material.renderQueue = -1;
			}
			if (culling) {
				material.SetInt("_Cul", (int)UnityEngine.Rendering.CullMode.Back);
				material.EnableKeyword("BFC");
			} else {
				material.SetInt("_Cul", (int)UnityEngine.Rendering.CullMode.Off);
				material.DisableKeyword("BFC");
			}
		}

		base.OnGUI(materialEditor, properties);
	}
}
