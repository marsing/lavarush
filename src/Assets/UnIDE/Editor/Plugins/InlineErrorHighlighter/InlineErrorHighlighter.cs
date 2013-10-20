using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.IO;

using CSharpParser.ProjectContent;
using CSharpParser.ProjectModel;


namespace UIDE.Plugins.InlineErrorHighlighter {
	public enum ErrorType {Error,Warning};
	public class ErrorDef:System.Object {
		public ErrorType type = ErrorType.Error;
		public string description;
		public int line;
		public int column;
	}
	
	public class InlineErrorHighlighter:UIDEPlugin {
		public bool wantsParserUpdate = false;
		private string tmpFileName = "UnIDEData/ParserTmp/Tmp.txt";
		public float reparseDelay = 0.5f;
		
		public List<ErrorDef> errors = new List<ErrorDef>();
		
		public ErrorDef mouseOverError = null;
		public Rect mouseOverRect;
		
		
		private GUIStyle errorDescriptionStyle;
		private GUIStyle outlineStyle;
		private GUIStyle shadowStyle;
		
		private Texture2D errorUnderlineTex;
		
		private float lastParseTime = 0.0f;
		
		public override void Start() {
			useCustomWindow = true;
			lastParseTime = editor.editorWindow.time;
			editor.onPreRenderLineCallbacks.Add(OnPreRenderLine);
			
			errorDescriptionStyle = editor.editorWindow.theme.GetStyle("ListItem");
			outlineStyle = editor.editorWindow.theme.GetStyle("BoxBG");
			shadowStyle = editor.editorWindow.theme.GetStyle("DropShadow");
			errorUnderlineTex = editor.editorWindow.theme.GetResourceTexture("ErrorUnderline");
			wantsParserUpdate = true;
		}
		
		public override void OnDestroy() {
			editor.onPreRenderLineCallbacks.Remove(OnPreRenderLine);
		}
		
		public override void OnTextEditorUpdate() {
			if (wantsParserUpdate && editor.editorWindow.time-lastParseTime > reparseDelay) {
				if (!UIDEThreadPool.IsRegistered("InlineErrorHighlighter_Parse")) {
					wantsParserUpdate = false;
					lastParseTime = editor.editorWindow.time;
					UIDEThreadPool.RegisterThread("InlineErrorHighlighter_Parse",UpdateParser);
				}
			}
			mouseOverError = null;
		}
		
		public override void OnPostEnterText(string text) {
			wantsParserUpdate = true;
		}
		
		public override void OnPostBackspace() {
			wantsParserUpdate = true;
		}
		
		public bool OnPreRenderLine(UIDELine line) {
			if (line == null) return false;
			ErrorDef errorDef = null;
			for (int i = 0; i < errors.Count; i++) {
				if (errors[i] != null && errors[i].line == line.index) {
					errorDef = errors[i];
				}
			}
			if (errorDef != null) {
				Rect logLineRect = editor.actualTextAreaRect;
				logLineRect.x = editor.doc.scroll.x;
				float columnOffset = line.GetScreenPosition(errorDef.column)*editor.charSize.x;
				float lineEnd = line.GetScreenPosition(line.rawText.Length)*editor.charSize.x;
				float highlightLegth = lineEnd-columnOffset;
				
				logLineRect.x += columnOffset;
				logLineRect.width = highlightLegth;
				logLineRect.height = editor.charSize.y;
				logLineRect.y = (line.index)*editor.charSize.y;
				
				if (logLineRect.Contains(Event.current.mousePosition)) {
					mouseOverError = errorDef;
					mouseOverRect = logLineRect;
					mouseOverRect.width = Mathf.Clamp(mouseOverRect.width,100.0f,editor.actualTextAreaRect.width);
				}
				
				Rect underlineRect = logLineRect;
				underlineRect.height = errorUnderlineTex.height;
				underlineRect.y += editor.charSize.y;
				underlineRect.y -= underlineRect.height;
				
				Rect underlineUVRect = underlineRect;
				underlineUVRect.x = 0;
				underlineUVRect.y = 0;
				underlineUVRect.height = 1;
				
				underlineUVRect.width = underlineUVRect.width/(float)errorUnderlineTex.width;
				
				Color color = editor.editorWindow.theme.errorUnderlineColor;
				
				GUI.color = color;
				GUI.DrawTextureWithTexCoords(underlineRect,errorUnderlineTex,underlineUVRect);
				GUI.color = Color.white;
			}
			return false;
		}
		
		public override void OnTextEditorGUI(int windowID) {
			if (mouseOverError != null) {
				Rect rect = mouseOverRect;
				rect.y -= editor.doc.scroll.y;
				rect.y += editor.tabRect.height;
				rect.x += editor.lineNumberWidth;
				
				GUIContent content = new GUIContent(mouseOverError.description);
				GUIStyle style = new GUIStyle(errorDescriptionStyle);
				style.wordWrap = true;
				
				style.padding.left = style.padding.right;
				
				GUIStyle styleNoWordWrap = new GUIStyle(style);
				styleNoWordWrap.wordWrap = false;
				
				rect.width = Mathf.Min(rect.width,styleNoWordWrap.CalcSize(content).x);
				
				rect.width += style.padding.left+style.padding.right+4;
				
				rect.height = style.CalcHeight(content,rect.width);
				
				rect.y += editor.charSize.y;
				
				GUI.Box(rect,"",shadowStyle);
				GUI.Box(rect,"",outlineStyle);
				GUI.Label(rect,content,style);
			}
		}
		
		public void UpdateParser(System.Object obj) {
			string text = editor.doc.GetRawText();
			
			if (!Directory.Exists(Path.GetDirectoryName(tmpFileName))) {
				Directory.CreateDirectory(Path.GetDirectoryName(tmpFileName));
			}
			try {
				System.IO.StreamWriter fileStream = new System.IO.StreamWriter(tmpFileName);
				fileStream.Write(text);
				fileStream.Close();
			}
			catch (System.Exception) {
				return;
			}
			
			CompilationUnit project = new CompilationUnit("");
			
			project.AddFile(tmpFileName);
			
			try {
				project.Parse();
			}
			catch (System.Exception) {
				return;
			}
			
			errors = new List<ErrorDef>();
			
			foreach (Error error in project.Errors) {
				if(error.Code != "SYNERR") continue;
				ErrorDef def = new ErrorDef();
				def.line = error.Line-1;
				def.column = error.Column-1;
				def.description = error.Description;
				def.type = ErrorType.Error;
				errors.Add(def);
				
				if (def.description == "scolon expected") {
					def.description = "';' expected.";
				}
				if (def.description == "lbrace expected") {
					def.description = "'{' expected.";
				}
				if (def.description == "rbrace expected") {
					def.description = "'}' expected.";
				}
			}
			
			wantsParserUpdate = false;
		}
		
		public override bool CheckIfShouldLoad(UIDETextEditor textEditor) {
			if (UIDEEditor.current == null) return false;
			if (Application.platform == RuntimePlatform.OSXEditor) {
				return false;
			}
			if (textEditor.extension.ToLower() == ".cs") {
				if (UIDEEditor.current.generalSettings.GetUseRealtimeErrorHighlighting()) {
					return true;
				}
			}
			return false;
		}
		
	}
}
