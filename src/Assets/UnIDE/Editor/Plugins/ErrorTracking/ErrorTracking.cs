using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using System.IO;

using UnityEditor;

namespace UIDE.Plugins.ErrorTracking {
	public class LogStackItem:System.Object {
		public string filename = "";
		public string contextString = "";
		public string contextFunction = "";
		public int line = 0;
		public int column = 0;
	}
	public class UIDELogEntry:System.Object {
		public string fileName;
		public int line;
		public int column;
		public string logType;
		public string message;
		public List<LogStackItem> stackItems = new List<LogStackItem>();
	}

	public class ErrorTracking:UIDEPlugin {
		static private bool expandStack = false;
		static private Vector2 callStackScroll;
		
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
		
		private int lastEntryIndex = -1;
		
		
		
		
		public bool isVisible {
			get {
				return (currentLogEntry != null) && !forceClosed;
			}
		}
		
		
		public override void Start() {
			//Debug.Log("sdf");
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
			
			if (entry.stackItems.Count > 0) {
				bool originalExpanded = expandStack;
				expandStack = EditorGUILayout.Foldout(expandStack,"Call Stack");
				if (expandStack != originalExpanded) {
					editor.editorWindow.Repaint();
				}
				if (expandStack) {
					callStackScroll = GUILayout.BeginScrollView(callStackScroll);
					GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
					buttonStyle.alignment = TextAnchor.MiddleLeft;
					for (int i = entry.stackItems.Count-1; i >= 0; i--) {
						LogStackItem item = entry.stackItems[i];
						if (GUILayout.Button(FormatLogStackItem(item),buttonStyle)) {
							if (File.Exists(item.filename)) {
								RequestGoToFileAndLine(item.filename,item.line-1, 0);
							}
						}
					}
					GUILayout.EndScrollView();
				}
			}
			
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
			
			if (entry.stackItems.Count > 0) {
				size.y += GUI.skin.label.CalcHeight(new GUIContent("Call Stack"),size.x);
				size.y += GUI.skin.label.margin.top+GUI.skin.label.margin.bottom;
				//expandStack = EditorGUILayout.Foldout(expandStack,"Call Stack");
				if (expandStack) {
					for (int i = Mathf.Min(entry.stackItems.Count-1,4-1); i >= 0; i--) {
						LogStackItem item = entry.stackItems[i];
						Vector2 s = GUI.skin.button.CalcSize(new GUIContent(FormatLogStackItem(item)));
						size.y += s.y;
						size.y += GUI.skin.button.margin.top+GUI.skin.label.margin.bottom;
						size.x = Mathf.Max(size.x,s.x);
						//GUILayout.Button(item.contextString+" "+Path.GetFileName(item.filename)+" "+item.line);
					}
				}
			}
			
			string buttonText = "Go there";
			size.y += GUI.skin.button.CalcHeight(new GUIContent(buttonText),size.x);
			size.y += GUI.skin.button.margin.top+GUI.skin.button.margin.bottom;
			
			size.x += boxStyle.padding.left+boxStyle.padding.right;
			size.y += boxStyle.padding.top+boxStyle.padding.bottom;
			
			size.x += closeButtonStyle.fixedWidth;
			
			size.y = Mathf.Min(size.y,editor.textEditorRect.height-editor.desiredTabBarHeight-editor.editorWindow.defaultSkin.horizontalScrollbar.fixedHeight);
			
			return size;
		}
		
		private string FormatLogStackItem(LogStackItem item) {
			string str = item.contextFunction+" "+Path.GetFileName(item.filename)+" "+item.line;
			return str;
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
			lastConsoleUpdateTime = Time.realtimeSinceStartup;
			#if UNITY_EDITOR
			System.Type type = null;
			if (consoleWindow == null) {
				type = System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
				FieldInfo fieldInfo = type.GetField("ms_ConsoleWindow",BindingFlags.NonPublic|BindingFlags.Static);
				consoleWindow = fieldInfo.GetValue(null);
			}
			
			if (consoleWindow == null) return;
			
			type = consoleWindow.GetType();
			
			FieldInfo listViewField = type.GetField("m_ListView",BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic);
			UnityEditor.ListViewState listView = (UnityEditor.ListViewState)listViewField.GetValue(consoleWindow);
			
			if (listView.row == -1) {
				lastEntryIndex = -1;
				return; //there is no selection
			}
			
			if (lastEntryIndex == listView.row) {
				return;
			}
			
			//Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.SceneView));
 			System.Type logEntriesType = System.Type.GetType("UnityEditorInternal.LogEntries,UnityEditor");
			MethodInfo method = logEntriesType.GetMethod("GetFirstTwoLinesEntryTextAndModeInternal");
			MethodInfo endMethod = logEntriesType.GetMethod("EndGettingEntries");
			MethodInfo startMethod = logEntriesType.GetMethod("StartGettingEntries");
			//MethodInfo method = logEntriesType.GetMethod("GetEntryInternal");
			object[] p = new object[3];
			p[0] = listView.row;
			p[1] = 0;
			p[2] = "";
			
			//object unityLogEntry = null;
			try {
				startMethod.Invoke(consoleWindow,new object[] {});
				method.Invoke(consoleWindow,p);
				endMethod.Invoke(consoleWindow,new object[] {});
			}
			finally {
				
			}
			
			int errorMode = (int)p[1];
			
			MethodInfo getIconForErrorModeMethod = type.GetMethod("GetIconForErrorMode",BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
			Texture2D activeContextIcon = (Texture2D)getIconForErrorModeMethod.Invoke(consoleWindow,new object[] {errorMode,true});
			
			
			FieldInfo activeTextField = type.GetField("m_ActiveText",BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic);
			string activeText = (string)activeTextField.GetValue(consoleWindow);
			
			if (lastConsoleRawText == activeText) return;
			lastConsoleRawText = activeText;
			string activeContext = activeText;
			
			currentFileLogEntry = null;
			currentLogEntry = null;
			forceClosed = false;
			wantsReapint = true;
			editor.editorWindow.Repaint();
			//lastConsoleRawText = activeContext;
			
			string regString = @"^(?<context>.*\(.*\)) \(at (?<file>[[FILEMATCH]]):(?<line>\d*)\)";
			regString = regString.Replace("[[FILEMATCH]]",@"(\w:)?(/)?(\w+/)*(\w+\.\w+)+");
			string[] lines = activeText.Split('\n');
			List<LogStackItem> stackItems = new List<LogStackItem>();
			LogStackItem bestStackItem = null;
			for (int i = lines.Length-1; i >= 0; i--) {
				
				Regex r = new Regex(regString);
				Match result = r.Match(lines[i]);
				if (result.Success) {
					
					string _context = result.Groups["context"].Value.ToString();
					string _file = result.Groups["file"].Value.ToString();
					string _line = result.Groups["line"].Value.ToString();
					
					LogStackItem item = new LogStackItem();
					item.filename = _file;
					//item.line = _line;
					int.TryParse(_line,out item.line);
					item.contextString = _context;
					if (_context.IndexOf(":") != -1 && !_context.EndsWith(":")) {
						item.contextFunction = _context.Substring(_context.IndexOf(":")+1);
					}
					stackItems.Add(item);
					if (System.IO.File.Exists(_file)) {
						bestStackItem = item;
					}
				}
			}
			
			if (bestStackItem != null) {
				
				LogStackItem item = bestStackItem;
				
				UIDELogEntry logEntry = new UIDELogEntry();
				logEntry.stackItems = stackItems;
				logEntry.fileName = item.filename;
				logEntry.line = item.line;
				logEntry.column = 0;
				logEntry.logType = "";
				logEntry.message = "";
				if (activeContextIcon != null) {
					if (activeContextIcon.name == "d_console.infoicon.sml" || activeContextIcon.name == "d_console.infoicon") {
						logEntry.logType = "Assert";
					}
					if (activeContextIcon.name == "d_console.erroricon.sml" || activeContextIcon.name == "d_console.erroricon") {
						logEntry.logType = "Runtime Error";
					}
				}
				if (logEntry.logType == "") {
					logEntry.logType = "Unknown Assert";
				}
				if (logEntry.fileName == editor.filePath) {
					currentFileLogEntry = logEntry;
				}
				currentLogEntry = logEntry;
				return;
			}
			
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
					if (activeContextIcon.name == "d_console.infoicon.sml" || activeContextIcon.name == "d_console.infoicon") {
						logType = "Assert";
					}
					if (activeContextIcon.name == "d_console.erroricon.sml" || activeContextIcon.name == "d_console.erroricon") {
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
				
				if (fileName == editor.filePath) {
					currentFileLogEntry = logEntry;
				}
				currentLogEntry = logEntry;
				
				return;
			}

			#endif
		}
	}
}
