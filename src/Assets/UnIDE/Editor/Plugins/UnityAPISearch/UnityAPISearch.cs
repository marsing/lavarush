using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//using UnityEditor;
using UIDE.RightClickMenu;

namespace UIDE.Plugins.UnityAPISearch {
	public class UnityAPISearch:UIDEPlugin {
		public static string searchURL = "http://docs.unity3d.com/Documentation/ScriptReference/30_search.html?q=";
		
		public override void OnPreTextEditorGUI() {
			if (editor.editorWindow.canTextEditorInteract) {
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F1) {
					OpenDocAtCurrentPosition();
				}
			}
		}
		
		public override RCMenuItem[] OnGatherRCMenuItems() {
			List<RCMenuItem> items = new List<RCMenuItem>();
			if (!editor.cursor.selection.hasSelection) return items.ToArray();
			
			RCMenuItem item = new RCMenuItem("Search Unity Docs",OpenDocRCCallback);
			items.Add(item);
			
			return items.ToArray();
		}
		
		private void OpenDocRCCallback(System.Object[] objs) {
			OpenDocAtCurrentPosition();
		}
		
		public void OpenDocAtCurrentPosition() {
			OpenDocAt(editor.cursor.GetVectorPosition());
		}
		public void OpenDocAt(Vector2 pos) {
			string term = "";
			if (editor.cursor.selection.hasSelection) {
				term = editor.cursor.selection.GetSelectedText();
			}
			else {
				Vector2 start = editor.ExpandToLogicalBoundStart(pos);
				Vector2 end = editor.ExpandToLogicalBoundEnd(pos);
				editor.cursor.selection.start = start;
				editor.cursor.selection.end = end;
				term = editor.cursor.selection.GetSelectedText();
			}
			
			string url = searchURL+term;
			//Help.ShowHelpPage(url);
			Application.OpenURL(url);
		}
	}
}