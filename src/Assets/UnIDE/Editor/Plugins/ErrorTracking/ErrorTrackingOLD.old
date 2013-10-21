using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UIDE.Plugins.ErrorTracking {
	public class ErrorTracking:UIDEPlugin {
		public bool anchorTop = true;
		
		private System.Object consoleWindow;
		private float lastConsoleUpdateTime = 0.0f;
		private string lastConsoleRawText = "-1";
		//private float lastEntryChangeTime = 0.0f;
		private UIDELogEntry currentFileLogEntry;
		private UIDELogEntry currentLogEntry;
		private GUIStyle lineErrorStyle;
		private GUIStyle boxStyle;
		private GUIStyle closeButtonStyle;
		private GUIStyle shadowStyle;
		private Vector2 desiredBoxSize = new Vector2(0,0);
		private Rect windowRect;
		private bool forceClosed = false;
		private bool wantsReapint = false;
		
		private bool hasGotoRequest = false;
		private string gotoFileName = "";
		private int gotoLine = 0;
		private int gotoColumn = 0;
		
		
		public bool isVisible {
			get {
				return (currentLogEntry != null) && !forceClosed;
			}
		}
		
		
		public override void Start() {
			//bool warning;
			useCustomWindow = true;
			lastConsoleRawText = "-1";
			editor.onPreRenderLineCallbacks.Add(OnPreRenderLine);
			lineErrorStyle = editor.editorWindow.theme.GetStyle("LineErrorBG");
			boxStyle = editor.editorWindow.theme.GetStyle("PopupWindowBackground");
			closeButtonStyle = editor.editorWindow.theme.GetStyle("CloseButton");
			shadowStyle = editor.editorWindow.theme.GetStyle("DropShadow");
		}
		
		public override void OnFocus() {
			
			lastConsoleUpdateTime = 0.0f;
			lastConsoleRawText = "-1";
			//forceClosed = false;
		}
		public override void OnSwitchToTab() {
			lastConsoleUpdateTime = 0.0f;
			lastConsoleRawText = "-1";
			//forceClosed = false;
		}
		
		public override void OnSavePerformed() {
			ClearLog();
		}
		
		public override void OnTextEditorUpdate() {
			if (hasGotoRequest) {
				GoToFileAndLine(gotoFileName,gotoLine,gotoColumn);
				hasGotoRequest = false;
			}
			if (wantsReapint) {
				editor.editorWindow.Repaint();
				wantsReapint = false;
			}
			if (Time.realtimeSinceStartup-lastConsoleUpdateTime > 0.1f) {
				UpdateSelectedConsoleEntry();
			}
			if (isVisible) {
				Rect clickBlocker = windowRect;
				clickBlocker.x -= editor.rect.x;
				clickBlocker.y -= editor.desiredTabBarHeight;
				editor.clickBlockers.Add(CreateClickBlocker(clickBlocker));
			}
		}
		
		public override void OnTextEditorGUI(int windowID) {
			if (!isVisible) return;
			GUI.skin = editor.editorWindow.theme.skin;
			
			Rect shadowRect = windowRect;
			shadowRect.x -= editor.rect.x;
			GUI.Box(shadowRect,"",shadowStyle);
			
			GUI.Window(windowID, windowRect, RenderMessage, "",boxStyle);
			GUI.BringWindowToFront(windowID);
		}
		
		public void RenderMessage(int windowID) {
			if (currentLogEntry == null) return;
			
			UIDELogEntry entry = currentLogEntry;
			if (currentFileLogEntry != null) {
				entry = currentFileLogEntry;
			}
			
			UpdateWindowRect(entry);
			
			string message = GetMessage(entry);
			
			GUILayout.BeginHorizontal();
			GUIStyle wordWrapLabel = new GUIStyle(GUI.skin.label);
			wordWrapLabel.wordWrap = true;
			
			GUILayout.Label(message,wordWrapLabel);
			
			if (GUILayout.Button("",closeButtonStyle)) {
				forceClosed = true;
			}
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Go there",GUILayout.ExpandWidth(false), GUILayout.MinWidth(100))) {
				RequestGoToFileAndLine(entry.fileName,entry.line-1, entry.column-1);
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		
		public void UpdateWindowRect(UIDELogEntry entry) {
			desiredBoxSize = CalcSize(entry);
			windowRect = editor.textEditorRect;
			windowRect.x = editor.rect.x;
			windowRect.width -= editor.editorWindow.defaultSkin.verticalScrollbar.fixedWidth;
			
			windowRect.x += windowRect.width*1f;
			windowRect.y += windowRect.height*1f;
			
			windowRect.width = Mathf.Min(windowRect.width,desiredBoxSize.x);
			windowRect.height = Mathf.Min(windowRect.height,desiredBoxSize.y);
			
			
			windowRect.x -= windowRect.width*1f;
			windowRect.y -= windowRect.height*1f;
			
			//windowRect.x -= editor.editorWindow.defaultSkin.verticalScrollbar.fixedWidth;
			windowRect.y -= editor.editorWindow.defaultSkin.horizontalScrollbar.fixedHeight;
			if (anchorTop) {
				windowRect.y = editor.desiredTabBarHeight;
			}
		}
		
		public Vector2 CalcSize(UIDELogEntry entry) {
			if (entry == null) return new Vector2(0,0);
			string message = GetMessage(entry);
			GUIContent content = new GUIContent(message);
			
			GUIStyle wordWrapLabel = new GUIStyle(GUI.skin.label);
			wordWrapLabel.wordWrap = true;
			
			float desiredWidth = editor.textEditorRect.width-editor.editorWindow.defaultSkin.verticalScrollbar.fixedWidth*2;
			float height = wordWrapLabel.CalcHeight(content,desiredWidth);
			
			height += GUI.skin.label.margin.top+GUI.skin.label.margin.bottom;
			
			GUIStyle noWordWrapStyle = new GUIStyle(GUI.skin.label);
			noWordWrapStyle.wordWrap = false;
			
			Vector2 size = noWordWrapStyle.CalcSize(content);
			size.x = Mathf.Min(desiredWidth,size.x);
			size.y = height;
			size.x += GUI.skin.label.margin.left+GUI.skin.label.margin.right;
			//size.x += GUI.skin.label.margin.left+GUI.skin.label.margin.right;
			
			string buttonText = "Go there";
			size.y += GUI.skin.button.CalcHeight(new GUIContent(buttonText),size.x);
			size.y += GUI.skin.button.margin.top+GUI.skin.button.margin.bottom;
			
			size.x += boxStyle.padding.left+boxStyle.padding.right;
			size.y += boxStyle.padding.top+boxStyle.padding.bottom;
			
			size.x += closeButtonStyle.fixedWidth;
			
			size.y = Mathf.Min(size.y,editor.textEditorRect.height-editor.desiredTabBarHeight-editor.editorWindow.defaultSkin.horizontalScrollbar.fixedHeight);
			
			return size;
		}
		
		public string GetMessage(UIDELogEntry entry) {
			if (entry == null) return "";
			bool isCurrentFile = false;
			if (entry == currentFileLogEntry) {
				isCurrentFile = true;
			}
			string message = "";
			if (entry.logType == "error") {
				message += "Error";
			}
			else if (entry.logType == "warning") {
				message += "Warning";
			}
			else {
				message += entry.logType;
			}
			message += " in:\n";
			if (isCurrentFile) {
				message += "This file\n";
			}
			else {
				message += entry.fileName+"\n";
			}
			message += "At line ";
			message += entry.line+", column "+entry.column+"\n";
			if (entry.message != "") {
				message += "Message:\n";
				message += entry.message+"";
			}
			return message;
		}
		
		public bool OnPreRenderLine(UIDELine line) {
			if (currentFileLogEntry != null && line.index == currentFileLogEntry.line-1) {
				Rect logLineRect = editor.actualTextAreaRect;
				logLineRect.x = editor.doc.scroll.x;
				logLineRect.height = editor.charSize.y;
				logLineRect.y = (line.index)*editor.charSize.y;
				
				string lowerLogType = currentFileLogEntry.logType.ToLower();
				GUIStyle style = lineErrorStyle;
				Color color = editor.editorWindow.theme.errorColor;
				if (lowerLogType == "warning") {
					color = editor.editorWindow.theme.warningColor;
				}
				else if (lowerLogType == "assert" || lowerLogType == "unknown assert") {
					color = editor.editorWindow.theme.assertColor;
				}
				GUI.color = color;
				GUI.Box(logLineRect,"",style);
				GUI.color = Color.white;
			}
			return false;
		}
		
		public void RequestGoToFileAndLine(string fileName, int line, int column) {
			gotoFileName = fileName;
			gotoLine = line;
			gotoColumn = column;
			hasGotoRequest = true;
		}
		public void GoToFileAndLine(string fileName, int line, int column) {
			UIDETextEditor te = editor.editorWindow.OpenOrFocusEditorFromFile(fileName);
			if (te != null) {
				int lineOffset = (int)((editor.actualTextAreaRect.height/editor.charSize.y)*0.5f);
				te.ScrollToLine(line,lineOffset);
				te.cursor.posY = line;
				te.cursor.posX = column;
			}
			hasGotoRequest = false;
		}
		
		public void ClearLog() {
			#if UNITY_EDITOR
			System.Type type = null;
			if (consoleWindow == null) {
				type = System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
				FieldInfo fieldInfo = type.GetField("ms_ConsoleWindow",BindingFlags.NonPublic|BindingFlags.Static);
				consoleWindow = fieldInfo.GetValue(null);
			}
			else {
				type = consoleWindow.GetType();
			}
			if (consoleWindow == null) return;
			MethodInfo onDisableMethod = type.GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic);
			onDisableMethod.Invoke(consoleWindow,new object[] {});
			MethodInfo onEnableMethod = type.GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic);
			onEnableMethod.Invoke(consoleWindow,new object[] {});
			MethodInfo setActiveMathod = type.GetMethod("SetActiveEntry",BindingFlags.Instance|BindingFlags.NonPublic);
			setActiveMathod.Invoke(consoleWindow,new object[] {null});
			#endif
		}
		public void UpdateSelectedConsoleEntry() {
			//Debug.Log("sdf");
			lastConsoleUpdateTime = Time.realtimeSinceStartup;
			#if UNITY_EDITOR
			System.Type type = null;
			//if (consoleWindow == null) {
				type = System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
				FieldInfo fieldInfo = type.GetField("ms_ConsoleWindow",BindingFlags.NonPublic|BindingFlags.Static);
				consoleWindow = fieldInfo.GetValue(null);
			//}
			//else {
			//	type = consoleWindow.GetType();
			//}
			if (consoleWindow == null) return;
			
			
			FieldInfo activeContextField = type.GetField("m_ActiveContext",BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic);
			string activeContext = (string)activeContextField.GetValue(consoleWindow);
			
			FieldInfo activeContextIconField = type.GetField("m_ActiveContextIcon",BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic);
			Texture2D activeContextIcon = (Texture2D)activeContextIconField.GetValue(consoleWindow);
			
			if (lastConsoleRawText == activeContext) return;
			
			FieldInfo activeTextField = type.GetField("m_ActiveText",BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic);
			string activeText = (string)activeTextField.GetValue(consoleWindow);
			
			currentFileLogEntry = null;
			currentLogEntry = null;
			forceClosed = false;
			wantsReapint = true;
			editor.editorWindow.Repaint();
			lastConsoleRawText = activeContext;
			
			//compiler error/warnings:
			Regex regex = new Regex(@"(?<filename>^Assets/(?:\w+/)*(?:\w+\.\w+)+)\((?:(?<line>\d+),(?<column>\d+))\): ((?<type>warning|error) )?\w+: (?<message>(\.|\n)*)");
			Match match = regex.Match(activeText);
			
			if (match.Success) {
				string fileName = match.Groups["filename"].Value;
				int line = int.Parse(match.Groups["line"].Value);
				int column = int.Parse(match.Groups["column"].Value);
				string logType = match.Groups["type"].Value;
				//string message = match.Groups["message"].Value;
				UIDELogEntry logEntry = new UIDELogEntry();
				logEntry.fileName = fileName;
				logEntry.line = line;
				logEntry.column = column;
				logEntry.logType = logType;
				logEntry.message = "";
				if (logEntry.logType == "") {
					logEntry.logType = "Unknown Assert";
				}
				
				if (fileName == editor.filePath) {
					currentFileLogEntry = logEntry;
				}
				currentLogEntry = logEntry;
				//lastEntryChangeTime = Time.realtimeSinceStartup;
				return;
			}
			
			Regex conRegex = new Regex(@"(?<type>Assert|Log|Error) in file: (?<filename>Assets/(?:\w+/)*(?:\w+\.\w+)+) at line: (?:(?<line>\d+))");
			Match conMatch = conRegex.Match(activeContext);
			if (conMatch.Success) {
				
				string fileName = conMatch.Groups["filename"].Value;
				int line = int.Parse(conMatch.Groups["line"].Value);
				int column = 0;
				string logType = conMatch.Groups["type"].Value;
				if (activeContextIcon != null) {
					if (activeContextIcon.name == "d_console.infoicon.sml") {
						logType = "Assert";
					}
					if (activeContextIcon.name == "d_console.erroricon.sml") {
						logType = "Runtime Error";
					}
				}
				//string message = activeText;
				UIDELogEntry logEntry = new UIDELogEntry();
				logEntry.fileName = fileName;
				logEntry.line = line;
				logEntry.column = column;
				logEntry.logType = logType;
				logEntry.message = "";
				if (logEntry.logType == "") {
					logEntry.logType = "Unknown Assert";
				}
				//d_console.infoicon.sml
				//d_console.erroricon.sml
				//Debug.Log(activeContextIcon);
				
				if (fileName == editor.filePath) {
					currentFileLogEntry = logEntry;
				}
				currentLogEntry = logEntry;
				//Match dum = null;
				//Debug.Log(dum.Index);
				return;
			}
			
			//Assets/UnIDE/Scripts/Windows/UIDETextEditor.cs(457,24): warning CS0219: The variable `activeText' is assigned but its value is never used
			//Assets/UnIDE/Scripts/Windows/UIDETextEditor.cs(447,23): error CS1525: Unexpected symbol `.', expecting `)', `,', `;', `[', or `='
			#endif
		}
	}
}
