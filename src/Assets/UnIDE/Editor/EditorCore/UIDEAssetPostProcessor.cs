using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

public class UIDEAssetPostProcessor:AssetPostprocessor {
    static void OnPostprocessAllAssets(string[] importedAssets,string[] deletedAssets,string[] movedAssets,string[] movedFromAssetPaths) {
		OnChangedAsset(importedAssets);
		OnChangedAsset(deletedAssets);
		OnChangedAsset(movedAssets);
		OnChangedAsset(movedFromAssetPaths);
    }
	
	static void OnChangedAsset(string[] importedAssets) {
        foreach (var str in importedAssets) {
			string pathToLower = str.ToLower();
			bool isScript = pathToLower.EndsWith(".cs");
			isScript |= pathToLower.EndsWith(".js");
			isScript |= pathToLower.EndsWith(".boo");
			
            if (isScript) {
				if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == typeof(UIDEEditorWindow)) {
					UIDEEditorWindow window = (UIDEEditorWindow)EditorWindow.focusedWindow;
					if (window != null) {
						window.OnReloadScript(str);
					}
				}
			}
		}
	}
}
