using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

#if UNITY_3_5
public class UIDEAssetModProcessor:AssetModificationProcessor {
#else
public class UIDEAssetModProcessor:UnityEditor.AssetModificationProcessor {
#endif
	static string[] OnWillCreateAssets(string[] paths) {
		return OnWillSaveOrCreateAssets(paths);
	}
	
	static string[] OnWillSaveAssets(string[] paths) {
		return OnWillSaveOrCreateAssets(paths);
	}
	
	static string[] OnWillSaveOrCreateAssets(string[] paths) {
		List<string> outPaths = new List<string>();
		for (int i = 0; i < paths.Length; i++) {
			bool isSceneAsset = paths[i].ToLower().EndsWith(".unity");
			
			if (isSceneAsset) {
				if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == typeof(UIDEEditorWindow)) {
					UIDEEditorWindow window = (UIDEEditorWindow)EditorWindow.focusedWindow;
					if (window) {
						window.OnRequestSave();
					}
					else {
						Debug.LogWarning("Something went wrong!");
					}
					continue;
				}
			}
			
			outPaths.Add(paths[i]);
		}
		return outPaths.ToArray();
	}
	
}
