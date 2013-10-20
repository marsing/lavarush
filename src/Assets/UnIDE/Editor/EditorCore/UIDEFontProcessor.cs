using UnityEngine;
using System.Collections;

using UnityEditor;

namespace UIDE {
	public class UIDEFontProcessor:System.Object {
		static public void Process(string path, int fontSize, bool forceDynamicFont35) {
			TrueTypeFontImporter importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(path);
			importer.fontSize = fontSize;
			#if UNITY_3_5
			importer.fontRenderMode = FontRenderMode.NoAntialiasing;
			if (forceDynamicFont35) {
				importer.fontTextureCase = FontTextureCase.Dynamic;
			}
			else {
				importer.fontTextureCase = FontTextureCase.Unicode;
			}
			#else
			importer.fontTextureCase = FontTextureCase.Dynamic;
			importer.fontRenderingMode = FontRenderingMode.HintedSmooth;
			#endif
			AssetDatabase.ImportAsset(path);
		}
	}
}