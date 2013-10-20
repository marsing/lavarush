using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

namespace UIDE.Plugins.FindAndReplace {
	public class FindAndReplace:UIDEPlugin {
		public float rectHeight = 50.0f;
		public float maxWidth = 700.0f;
		public Rect rect;
		public Rect rectZeroPos;
		public Rect textEditorRect;
		public Rect textEditorRectZeroPos;
		public GUISkin skin;
		
		private int windowID = -1;
		
		private bool wantsToShow = false;
		public bool show {get; private set;}
		public bool shouldFocusFindField = false;
		public bool shouldFocusReplaceField = false;
		
		public bool useCase = false;
		public string findTerm = "";
		public string replaceTerm = "";
		
		
		public override void Start() {
			useCustomWindow = true;
			editor.onPostRenderLineCallbacks.Add(OnPostRenderLine);
		}
		
		public override void OnTextEditorUpdate() {
			if (wantsToShow != show) {
				show = wantsToShow;
				editor.editorWindow.Repaint();
			}
			
			if (show) {
				Rect clickBlocker = rect;
				clickBlocker.x += editor.lineNumberWidth;
				clickBlocker.y += editor.desiredTabBarHeight;
				editor.clickBlockers.Add(CreateClickBlocker(clickBlocker));
			}
		}
		
		public override void OnTextEditorGUI(int windowID) {
			this.windowID = windowID;
			if (!show) return;
			
			GUI.color = new Color(1,1,1,1);
			
			if (!editor.TestClickBlockers(editor.windowMousePos,this)) {
				GUI.BringWindowToFront(windowID);
				if (Event.current.type == EventType.MouseDown) {
					FocusWindow();
				}
			}
			
			skin = editor.editorWindow.theme.skin;
			
			textEditorRect = editor.GetTextEditorRect();
			textEditorRect.y += editor.tabRect.height;
			textEditorRect.height -= editor.tabRect.height;
			textEditorRectZeroPos = textEditorRect;
			textEditorRectZeroPos.x = 0;
			textEditorRectZeroPos.y = 0;
			
			UpdateRect();
			
			GUI.skin = skin;
			
			Rect winRect = rect;
			winRect.x += textEditorRect.x;
			winRect.y += textEditorRect.y;
			
			Rect shadowClipRect = textEditorRect;
			shadowClipRect.x = 0;
			GUI.BeginGroup(shadowClipRect);
			GUIStyle shadowStyle = skin.GetStyle("DropShadow");
			if (shadowStyle != null) {
				Rect shadowRect = rect;
				shadowRect.x += editor.lineNumberWidth;
				GUI.Box(shadowRect,"",shadowStyle);
			}
			GUI.EndGroup();
			
			GUI.BeginGroup(textEditorRect);
			GUI.Window(windowID, winRect, DrawGUIWindow, "Find And Replace", new GUIStyle());
			GUI.EndGroup();
			
			GUI.skin = null;
		}
		
		public void DrawGUIWindow(int id) {
			DrawGUI();
		}
		
		public override void OnPreTextEditorGUI() {
			DetectHotkeys();
		}
		
		public void Show() {
			wantsToShow = true;
			editor.editorWindow.Repaint();
		}
		public void Hide() {
			wantsToShow = false;
			shouldFocusFindField = false;
			shouldFocusReplaceField = false;
			editor.editorWindow.Repaint();
		}
		
		public void FocusWindow() {
			if(show) {
				GUI.BringWindowToFront(windowID);
				GUI.FocusWindow(windowID);
			}
		}
		
		public void DetectHotkeys() {
			//bool controlPressed = Event.current.control || Event.current.command;
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F3) {
				if (Event.current.shift) {
					GotoPrevious(true);
				}
				else {
					GotoNext(true);
				}
				
				Event.current.Use();
			}
			
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Find") {
				Show();
				shouldFocusFindField = true;
				FocusWindow();
				editor.editorWindow.Repaint();
				Event.current.Use();
			}
		}
		
		public void OnPostRenderLine(UIDELine line) {
			if (!show) return;
			if (findTerm != "") {
				string lineText = line.rawText;
				string actualFindTerm = findTerm;
				string actualLineText = line.rawText;
				
				if (!useCase) {
					actualFindTerm = actualFindTerm.ToLower();
					actualLineText = actualLineText.ToLower();
				}
				
				List<int> searchIndexes = AllIndexesOf(actualLineText,actualFindTerm);
				if (searchIndexes.Count > 0) {
					float yPos = line.index*editor.charSize.y;
					GUIStyle hoverHighlightStyle = editor.editorWindow.theme.GetStyle("CursorHoverHighlight");
					//float contentHeight = hoverHighlightStyle.CalcHeight(new GUIContent("#YOLO"),100);
					for (int i = 0; i < searchIndexes.Count; i++) {
						int index = searchIndexes[i];
						int charXPos = line.GetScreenPosition(index);
						float xPos = charXPos*editor.charSize.x;
						//float xEndPos = line.GetScreenPosition(index+findTerm.Length)*editor.charSize.x;
						
						bool isSameAsSelection = false;
						if (editor.cursor.selection.actualStart == new Vector2(index,line.index) && editor.cursor.selection.actualEnd == new Vector2(index+findTerm.Length,line.index)) {
							isSameAsSelection = true;
						}
						string inlineText = lineText.Substring(index,findTerm.Length);
						if (!isSameAsSelection) {
							UIDEElement element = new UIDEElement();
							element.rawText = inlineText;
							element.line = line;
							
							editor.RenderElement(element, new Vector2(xPos,yPos),index,hoverHighlightStyle,false);
							//float width = xEndPos-xPos;
							//Rect r = new Rect(xPos,yPos,width,contentHeight);
							//GUI.Box(r,"",hoverHighlightStyle);
						}
					}
				}
			}
			
		}
		
		public void ReplaceNext(bool wrap) {
			if (editor.cursor.selection.hasSelection) {
				Vector2 pos = editor.cursor.selection.actualStart;
				editor.cursor.posX = (int)pos.x;
				editor.cursor.posY = (int)pos.y;
				editor.cursor.selection.start = editor.cursor.GetVectorPosition();
				editor.cursor.selection.end = editor.cursor.GetVectorPosition();
			}
			
			GotoNext(wrap);
			string undoName = "Replace "+editor.undoManager.GetUniqueID();
			editor.DeleteSelection(undoName);
			editor.EnterText(replaceTerm,undoName);
		}
		public void ReplacePrevious(bool wrap) {
			if (editor.cursor.selection.hasSelection) {
				Vector2 pos = editor.cursor.selection.actualEnd;
				editor.cursor.posX = (int)pos.x;
				editor.cursor.posY = (int)pos.y;
				editor.cursor.selection.start = editor.cursor.GetVectorPosition();
				editor.cursor.selection.end = editor.cursor.GetVectorPosition();
			}
			GotoPrevious(wrap);
			string undoName = "Replace "+editor.undoManager.GetUniqueID();
			editor.DeleteSelection(undoName);
			Vector2 startPos = editor.cursor.GetVectorPosition();
			
			editor.EnterText(replaceTerm,undoName);
			
			editor.cursor.posX = (int)startPos.x;
			editor.cursor.posY = (int)startPos.y;
		}
		
		public void GotoNext(bool wrap) {
			if (findTerm == "") return;
			Vector2 pos = editor.cursor.GetVectorPosition();
			if (editor.cursor.selection.hasSelection) {
				pos = editor.cursor.selection.actualEnd;
			}
			Vector2 newPos = FindNext(pos, 1, wrap);
			if (newPos != new Vector2(-1,-1)) {
				editor.cursor.selection.start = newPos;
				
				Vector2 endPos = newPos;
				endPos.x += findTerm.Length;
				editor.cursor.selection.end = endPos;
				
				editor.cursor.posX = (int)endPos.x;
				editor.cursor.posY = (int)endPos.y;
				//editor.cursor.selection.start = endPos;
				//editor.cursor.selection.end = endPos;
				editor.ScrollToLine(editor.cursor.posY,10);
				editor.ScrollToColumn(editor.cursor.posY,(int)editor.cursor.selection.actualEnd.x,10);
				editor.ScrollToColumn(editor.cursor.posY,(int)editor.cursor.selection.actualStart.x,10);
			}
			
			FocusWindow();
			editor.editorWindow.Repaint();
		}
		
		public void GotoPrevious(bool wrap) {
			if (findTerm == "") return;
			Vector2 pos = editor.cursor.GetVectorPosition();
			if (editor.cursor.selection.hasSelection) {
				pos = editor.cursor.selection.actualStart;
				if (findTerm.Length == 1) {
					pos = editor.doc.IncrementPosition(pos,-1);
				}
			}
			Vector2 newPos = FindNext(pos, -1, wrap);
			if (newPos != new Vector2(-1,-1)) {
				editor.cursor.selection.start = newPos;
				
				Vector2 endPos = newPos;
				endPos.x += findTerm.Length;
				editor.cursor.selection.end = endPos;
				
				editor.cursor.posX = (int)newPos.x;
				editor.cursor.posY = (int)newPos.y;
				
				editor.ScrollToLine(editor.cursor.posY,10);
				editor.ScrollToColumn(editor.cursor.posY,(int)editor.cursor.selection.actualEnd.x,10);
				editor.ScrollToColumn(editor.cursor.posY,(int)editor.cursor.selection.actualStart.x,10);
			}
			
			FocusWindow();
			editor.editorWindow.Repaint();
		}
		
		public Vector2 FindNext(Vector2 pos, int dir, bool wrap) {
			Vector2 outPos = new Vector2(-1,-1);
			if (dir == 0) {
				return outPos;
			}
			if (findTerm == "") return outPos;
			
			dir = (int)Mathf.Sign(dir);
			string actualFindTerm = findTerm;
			if (!useCase) {
				actualFindTerm = actualFindTerm.ToLower();
			}
			
			int startLine = (int)pos.y;
			int lineToStopAt = editor.actualLinesToRender.Count-1;
			if (dir == -1) {
				lineToStopAt = 0;
			}
			bool hasWrapped = false;
			
			for (int i = (int)pos.y; i < editor.actualLinesToRender.Count; i += dir) {
				UIDELine line = editor.doc.LineAt(i);
				string actualLineText = line.rawText;
				if (!useCase) {
					actualLineText = actualLineText.ToLower();
				}
				
				int index = -1;
				if (dir == 1) {
					index = actualLineText.IndexOf(actualFindTerm,(int)pos.x);
				}
				else {
					index = actualLineText.LastIndexOf(actualFindTerm,(int)pos.x);
				}
				if (index != -1) {
					outPos = new Vector2(index,i);
					break;
				}
				
				bool lineIsStopLine = false;
				if (dir == 1) {
					lineIsStopLine = i >= lineToStopAt;
				}
				else {
					lineIsStopLine = i <= lineToStopAt;
				}
				if (wrap && lineIsStopLine) {
					if (hasWrapped) {
						break;
					}
					if (dir == 1) {
						i = 0;
					}
					else {
						i = editor.actualLinesToRender.Count-1;
					}
					lineToStopAt = startLine;
					hasWrapped = true;
				}
				if (dir == 1) {
					pos.x = 0;
				}
				else {
					if (i-1 < 0) break;
					UIDELine nextLine = editor.doc.LineAt(i-1);
					pos.x = nextLine.rawText.Length;
				}
			}
			return outPos;
		}
		
		public void UpdateRect() {
			rect = textEditorRectZeroPos;
			if (maxWidth > 0.0f) {
				rect.width = Mathf.Min(rect.width,maxWidth);
			}
			rectHeight = GetDesiredHeight();
			rect.y = rect.height-rectHeight;
			rect.height = rectHeight;
			rect.x = textEditorRect.width/2.0f;
			rect.x -= rect.width/2.0f;
			rectZeroPos = rect;
			rectZeroPos.x = 0;
			rectZeroPos.y = 0;
		}
		
		public float GetDesiredHeight() {
			float textFieldHeight = skin.GetStyle("TextField").CalcHeight(new GUIContent("#YOLO"),200)*2;
			textFieldHeight += skin.textField.margin.top*2;
			textFieldHeight += skin.textField.margin.bottom*2;
			return textFieldHeight;
		}
		
		public void DrawGUI() {
			GUIStyle boxStyle = skin.box;
			GUIStyle textFieldStyle = skin.GetStyle("TextField");
			GUIStyle closeButtonStyle = skin.GetStyle("CloseButton");
			
			Rect extendedRect = rectZeroPos;
			extendedRect.height += 1;
			GUI.Box(extendedRect,"",boxStyle);
			
			GUILayout.BeginArea(rectZeroPos);
			float textFieldHeight = textFieldStyle.CalcHeight(new GUIContent("#YOLO"),200);
			
			GUILayout.BeginHorizontal();
			
			GUILayout.BeginVertical();
			GUILayout.Space(skin.textField.margin.top);
			GUILayout.Label("Find", GUILayout.Height(textFieldHeight), GUILayout.ExpandWidth(false));
			GUILayout.Space(skin.textField.margin.top);
			GUILayout.Label("Replace", GUILayout.Height(textFieldHeight), GUILayout.ExpandWidth(false));
			GUILayout.EndVertical();
			
			GUILayout.BeginVertical();
			GUI.SetNextControlName("FindReplace_FindField");
			findTerm =  EditorGUILayout.TextField(findTerm,textFieldStyle,GUILayout.Height(textFieldHeight),GUILayout.MaxWidth(1000000));
			GUI.SetNextControlName("FindReplace_ReplaceField");
			replaceTerm = EditorGUILayout.TextField(replaceTerm,textFieldStyle,GUILayout.Height(textFieldHeight),GUILayout.MaxWidth(1000000));
			GUILayout.EndVertical();
			
			GUILayout.Space(2);
			
			GUILayout.BeginVertical();
			if (GUILayout.Button("Find Previous",GUILayout.Height(textFieldHeight))) {
				GotoPrevious(true);
			}
			if (GUILayout.Button("Replace Previous",GUILayout.Height(textFieldHeight))) {
				ReplacePrevious(true);
			}
			GUILayout.EndVertical();
			
			GUILayout.BeginVertical();
			//Vector2 replaceNextButtonSize = skin.button.CalcSize(new GUIContent("Replace Next"));
			if (GUILayout.Button("Find Next",GUILayout.Height(textFieldHeight))) {
				GotoNext(true);
			}
			if (GUILayout.Button("Replace Next",GUILayout.Height(textFieldHeight))) {
				ReplaceNext(true);
			}
			GUILayout.EndVertical();
			
			if (GUILayout.Button("",closeButtonStyle,GUILayout.Height(textFieldHeight))) {
				Hide();
			}
			
			GUILayout.Space(3);
			
			GUILayout.EndHorizontal();
			
			GUILayout.EndArea();
			
			
			if (shouldFocusFindField) {
				GUI.FocusControl("FindReplace_FindField");
				shouldFocusFindField = false;
			}
			if (shouldFocusReplaceField) {
				GUI.FocusControl("FindReplace_ReplaceField");
				shouldFocusReplaceField = false;
			}
			
			
			if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)) {
				if (GUI.GetNameOfFocusedControl() == "FindReplace_FindField") {
					GotoNext(true);
					GUI.FocusControl("UIDETextAreaDummy");
					shouldFocusFindField = true;
					Event.current.Use();
				}
				if (GUI.GetNameOfFocusedControl() == "FindReplace_ReplaceField") {
					ReplaceNext(true);
					GUI.FocusControl("UIDETextAreaDummy");
					shouldFocusReplaceField = true;
					Event.current.Use();
				}
				//Event.current.Use();
			}
			
			//Hack for tabs to play nicely with the text editor
			if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t')) {
				if (GUI.GetNameOfFocusedControl() == "FindReplace_FindField") {
					Event.current.Use();
				}
				if (GUI.GetNameOfFocusedControl() == "FindReplace_ReplaceField") {
					Event.current.Use();
				}
				if (GUI.GetNameOfFocusedControl() == "UIDETextAreaDummy") {
					GUI.FocusControl("UIDETextAreaDummy");
					Event.current.Use();
				}
				//Event.current.Use();
			}
			
		}
		
		public static List<int> AllIndexesOf(string source, string value) {
			List<int> indexes = new List<int>(); 
			for (int index = 0;; index += value.Length) {
				index = source.IndexOf(value, index);
				if (index == -1)
					break;
				indexes.Add(index);
			}
			return indexes;
		}
		
	}
}
