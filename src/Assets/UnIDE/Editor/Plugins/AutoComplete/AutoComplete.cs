using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using System;

using UIDE.CodeCompletion;
using UIDE.SyntaxRules;
using UIDE.SyntaxRules.ExpressionResolvers;
using UIDE.RightClickMenu;

namespace UIDE.Plugins.AutoComplete {
	[System.Serializable]
	public class AutoComplete:UIDEPlugin {
		
		public GUISkin skin {
			get {
				return editor.editorWindow.theme.skin;
			}
		}
		public float boxWidth = 200;
		public float boxExpandedWidth = 0.0f;
		public int maxHeight = 8;
		public bool visible = false;
		public bool isDot = false;
		public bool isWord = false;
		public bool isWhiteSpace = false;
		public Vector2 scroll;
		public bool hasManualSelection = false;
		public int selectedIndex = 0;
		public bool genericMode {
			get {
				return editor.syntaxRule.useGenericAutoComplete;
			}
		}
		public bool isEnabled {
			get {
				if (editor.editorWindow.generalSettings.GetDisableCompletion()) {
					return false;
				}
				return true;
			}
		}
		
		public UIDEAutoCompleteData data;
		
		public Rect rect;
		
		private Rect tooltipRect;
		private bool showTooltip = false;
		private float toolTipMaxWidth = 500.0f;
		private TooltipItem tooltipSingle = null;
		
		private bool cancelTooltip = false;
		private bool wantsTooltipRectUpdate = false;
		private bool wantsTooltipUpdate = false;
		private bool wantsTooltipUpdateOverloads = false;
		private bool dontShowToolTipAgain = false;
		private TooltipItem[] tooltipMethodOverloads = new TooltipItem[0];
		private int tooltipMethodOverloadIndex = 0;
		private bool isShowingMethodOverloadTooltip {
			get {
				if (showTooltip && tooltipMethodOverloads.Length > 0) {
					return true;
				}
				return false;
			}
		}
		private TooltipItem currentTooltip {
			get {
				if (tooltipMethodOverloads.Length > 0) {
					return tooltipMethodOverloads[tooltipMethodOverloadIndex];
				}
				if (tooltipSingle != null) {
					return tooltipSingle;
				}
				return null;
			}
		}
		private string tooltipText {
			get {
				if (currentTooltip != null) {
					if (tooltipMethodOverloads.Length > 0) {
						return tooltipMethodOverloadPrefix+currentTooltip.text;
					}
					return currentTooltip.text;
				}
				return "";
			}
		}
		private string tooltipMethodOverloadPrefix {
			get {
				char upArrow = '\u25B2';
				char downArrow = '\u25BC';
				return upArrow+""+(tooltipMethodOverloadIndex+1)+"/"+tooltipMethodOverloads.Length+""+downArrow;
			}
		}
		private Vector2 toolTipPos;
		private Vector2 lastMousePos;
		private float lastMouseMoveTime;
		private float tooltipPopupTime = 1.0f;
		private Vector2 lastScroll;
		
		private bool holdAutoCompleteUpdate = false;
		private bool wantsAutoCompleteUpdate = false;
		private bool wantsFunishedUpdateAutoComplete = false;
		private bool wantsChainUpdate = false;
		
		
		private bool newVisibleState = false;
		private string autoCompleteKey = "";
		
		private bool useMultiThreading = true;
		//private Thread autoCompleteThread;
		private List<CompletionItem> itemList = new List<CompletionItem>();
		
		public override void Start() {
			data = editor.editorWindow.pluginSettings.GetOrCreatePluginData<UIDEAutoCompleteData>();
		}
		
		public override void OnTextEditorUpdate() {
			if (!isEnabled) return;
			
			if (newVisibleState != visible) {
				visible = newVisibleState;
				editor.editorWindow.Repaint();
			}
			
			if (visible) {
				Rect blockRect = rect;
				blockRect.x -= editor.doc.scroll.x;
				blockRect.y -= editor.doc.scroll.y;
				editor.clickBlockers.Add(CreateClickBlocker(blockRect));
				
				editor.disabledKeys.Add(KeyCode.UpArrow);
				editor.disabledKeys.Add(KeyCode.DownArrow);
			}
			if (isShowingMethodOverloadTooltip) {
				editor.disabledKeys.Add(KeyCode.UpArrow);
				editor.disabledKeys.Add(KeyCode.DownArrow);
			}
			if (wantsAutoCompleteUpdate) {
				if (TryStartUpdateAutoCompleteList(wantsChainUpdate)) {
					wantsAutoCompleteUpdate = false;
				}
			}
			if (wantsFunishedUpdateAutoComplete) {
				OnFinishUpdateAutoCompleteListActual();
			}
			
			if (wantsTooltipUpdate) {
				if (StartShowTooltip(toolTipPos,wantsTooltipUpdateOverloads)) {
					wantsTooltipUpdate = false;
					wantsTooltipUpdateOverloads = false;
				}
			}
			
			if (lastScroll != editor.doc.scroll) {
				HideToolTip();
				lastScroll = editor.doc.scroll;
			}
			
			if (!genericMode) {
				if (lastMousePos != editor.windowMousePos || !editor.editorWindow.canTextEditorInteract) {
					if (showTooltip && !isShowingMethodOverloadTooltip) {
						HideToolTip();
					}
					dontShowToolTipAgain = false;
					cancelTooltip = false;
					lastMouseMoveTime = Time.realtimeSinceStartup;
				}
				else {
					if (Time.realtimeSinceStartup-lastMouseMoveTime >= tooltipPopupTime && editor.textEditorNoScrollBarRect.Contains(editor.windowMousePosWithTabBar)) {
						if (!showTooltip && !dontShowToolTipAgain && (!UIDEThreadPool.IsRegistered("AutoComplete_UpdateTooltip")) && !wantsTooltipUpdate) {
							if (editor.TestClickBlockers(editor.windowMousePos)) {
								Vector2 cursorPos = editor.ScreenSpaceToCursorSpace(editor.windowMousePos);
								bool isValidChar = char.IsLetterOrDigit(editor.doc.GetCharAt(cursorPos));
								if (isValidChar) {
									StartShowTooltip(cursorPos,false);
								}
							}
							dontShowToolTipAgain = true;
						}
					}
				}
			}
			lastMousePos = editor.windowMousePos;
		}
		
		public override string OnPreEnterText(string text) {
			if (!isEnabled) return text;
			bool isSubmit = (text == "\n" || text == "\r" || text == "\t");
			if (isSubmit && text == "\n") {
				if (data.submitOnEnterMode == 2) {
					isSubmit = editor.syntaxRule.autoCompleteSubmitOnEnter;
					if (!isSubmit) {
						HideBox();
					}
				}
				else if (data.submitOnEnterMode == 0) {
					isSubmit = false;
					HideBox();
				}
			}
			if (isSubmit) {
				if (visible) {
					text = "";
					OnSubmit();
				}
				else {
					return text;
				}
			}
			
	
			return text;
		}
		
		public void OnSubmit() {
			HideBox();
			UIDEElement element = GetCursorElement();
			if (element != null && selectedIndex >= 0 && selectedIndex < itemList.Count) {
				Vector2 oldCursorPos = editor.cursor.GetVectorPosition();
				string originalText = element.line.rawText;
				
				if (element.tokenDef.HasType("WhiteSpace")) {
					string newText = itemList[selectedIndex].displayName;
					newText = element.line.rawText.Insert(editor.cursor.posX,newText);
					element.line.rawText = newText;
					editor.cursor.posX += itemList[selectedIndex].displayName.Length;
				}
				else {
					string newText = itemList[selectedIndex].displayName;
					if (element.rawText.Length == 1 && element.tokenDef.HasType("Dot")) {
						newText = element.rawText+newText;
					}
					int lengthDif = newText.Length-element.rawText.Length;
					
					element.line.SetElementText(element,newText);
					editor.cursor.posX += lengthDif;
				}
				element.line.RebuildElements();
				
				
				UIDEUndoManager undoManager = editor.undoManager;
				Vector2 newCursorPos = editor.cursor.GetVectorPosition();
				
				string undoName = "AutoComplete Submit "+undoManager.GetUniqueID();
	
				undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,element.line.index,originalText,element.line.rawText,oldCursorPos,newCursorPos);
			}
			
		}
		
		public void ShowBox() {
			UpdateRect();
			hasManualSelection = false;
			//selectedIndex = 0;
			rect.height = 0;
			boxExpandedWidth = 0.0f;
			newVisibleState = true;
		}
		public void HideBox() {
			hasManualSelection = false;
			//selectedIndex = 0;
			rect.height = 0;
			boxExpandedWidth = 0.0f;
			newVisibleState = false;
		}
		
		public void ShowToolTip(TooltipItem item) {
			showTooltip = true;
			tooltipSingle = item;
			tooltipMethodOverloads = new TooltipItem[0];
			tooltipMethodOverloadIndex = 0;
			wantsTooltipRectUpdate = true;
		}
		
		public void ShowToolTip(TooltipItem[] items) {
			showTooltip = true;
			tooltipSingle = null;
			tooltipMethodOverloads = items;
			tooltipMethodOverloadIndex = 0;
			wantsTooltipRectUpdate = true;
		}
		
		public void HideToolTip() {
			showTooltip = false;
		}
		
		public override void OnTextEditorCanClickChanged() {
			//if (editor.enableClick) {
			//	HideBox();
			//}
		}
		
		public override void OnPreTextEditorGUI() {
			if (!isEnabled) return;
			if (visible) {
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
					HideBox();
				}
				if (editor.ClickTestClickBlockers(editor.windowMousePos)) {
					HideBox();
				}
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftArrow) {
					HideBox();
					HideToolTip();
				}
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightArrow) {
					HideBox();
					HideToolTip();
				}
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.UpArrow) {
					if (selectedIndex > 0 && itemList.Count > 0) {
						selectedIndex -= 1;
						//selectedText = itemList[selectedIndex].text;
						ScrollToItem(selectedIndex);
						hasManualSelection = true;
					}
				}
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.DownArrow) {
					if (selectedIndex < itemList.Count-1 && itemList.Count > 0) {
						selectedIndex += 1;
						//selectedText = itemList[selectedIndex].text;
						ScrollToItem(selectedIndex);
						hasManualSelection = true;
					}
				}
			}
			
			if (isShowingMethodOverloadTooltip) {
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftArrow) {
					HideToolTip();
				}
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightArrow) {
					HideToolTip();
				}
				if (Event.current.type == EventType.MouseDown) {
					HideToolTip();
				}
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.UpArrow) {
					tooltipMethodOverloadIndex++;
					if (tooltipMethodOverloadIndex >= tooltipMethodOverloads.Length) {
						tooltipMethodOverloadIndex = 0;
					}
					wantsTooltipRectUpdate = true;
				}
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.DownArrow) {
					tooltipMethodOverloadIndex--;
					if (tooltipMethodOverloadIndex < 0) {
						tooltipMethodOverloadIndex = tooltipMethodOverloads.Length-1;
					}
					wantsTooltipRectUpdate = true;
				}
			}
		}
		
		public void FocusOnItem(int index) {
			if (itemList.Count == 0) return;
			index = Mathf.Clamp(index,0,itemList.Count);
			float itemHeight = GetItemHeight();
			float newY = scroll.y;
			newY = (index*itemHeight+itemHeight)-(rect.height/2.0f);
			
			if (newY != scroll.y) {
				editor.editorWindow.Repaint();
			}
			scroll.y = newY;
		}
		
		public void ScrollToItem(int index) {
			if (itemList.Count == 0) return;
			index = Mathf.Clamp(index,0,itemList.Count);
			float itemHeight = GetItemHeight();
			float desiredY = index*itemHeight;
			float newY = scroll.y;
			
			if (desiredY < scroll.y) {
				newY = index*itemHeight;
			}
			else if (desiredY > (scroll.y+rect.height)-itemHeight) {
				newY = ((index*itemHeight)-rect.height)+itemHeight;
			}
			
			if (newY != scroll.y) {
				editor.editorWindow.Repaint();
			}
			scroll.y = newY;
		}
		
		public override RCMenuItem[] OnGatherRCMenuItems() {
			List<RCMenuItem> items = new List<RCMenuItem>();
			if (!isEnabled) return items.ToArray();
			
			TooltipItem ttItem = editor.syntaxRule.GetTooltipItem(editor.ScreenSpaceToCursorSpace(editor.windowMousePos));
			if (ttItem == null) return items.ToArray();
			if (ttItem.item == null) return items.ToArray();
			
			if (!ttItem.item.hasDeclaredPosition) return items.ToArray();
			
			
			Vector2 pos = ttItem.item.declaredPosition;
			
			RCMenuItem item = new RCMenuItem("Go To Declaration",GoToDeclaration, new System.Object[] {pos});
			//item.SetCallback(GoToDeclaration);
			items.Add(item);
			
			return items.ToArray();
		}
		
		public void GoToDeclaration(System.Object[] posObj) {
			if (posObj.Length != 1) return;
			if (posObj[0].GetType() != typeof(Vector2)) return;
			Vector2 pos = (Vector2)posObj[0];
			int line = (int)pos.y;
			UIDETextEditor te = editor;
			if (te != null) {
				int lineOffset = (int)((editor.actualTextAreaRect.height/editor.charSize.y)*0.5f);
				te.ScrollToLine(line,lineOffset);
				te.cursor.posY = line;
				te.cursor.posX = (int)pos.x;
			}
		}
	
		public override void OnPostEnterText(string text) {
			if (!isEnabled) return;
			UpdateAutoComplete(text,false);
		}
		
		public override void OnPostBackspace() {
			if (!isEnabled) return;
			UpdateAutoComplete("",true);
		}
		public void UpdateAutoComplete(string text, bool isBackspace) {
			if (!isEnabled) return;
			if (text == "\n" || text == "\b" || (text == "" && !isBackspace)) {
				return;
			}
			
			if (text.Length > 1) {
				HideBox();
				return;
			}
			
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			UIDEElement element = GetCursorElement();
			
			if (!genericMode) {
				if (text == "(") {
					StartShowTooltip(editor.cursor.GetVectorPosition(),true);
					dontShowToolTipAgain = true;
					cancelTooltip = false;
				}
				if (text == ")" || isBackspace) {
					if (isShowingMethodOverloadTooltip) {
						HideToolTip();
					}
					if (!isBackspace) {
						cancelTooltip = true;
					}
				}
				if (text == " " && (editor.extension == ".cs" || editor.extension == ".js")) {
					Vector2 lastWordPos = editor.doc.IncrementPosition(editor.cursor.GetVectorPosition(),-1);
					lastWordPos = editor.doc.IncrementPosition(lastWordPos,-1);
					//lastWordPos = editor.doc.GoToEndOfWhitespace(lastWordPos,-1);
					UIDEElement newElement = editor.doc.GetElementAt(lastWordPos);
					if (newElement != null && newElement.rawText == "new") {
						Vector2 newWordStart = lastWordPos;
						newWordStart.x = line.GetElementStartPos(newElement);
						Vector2 leadingCharPos = editor.doc.IncrementPosition(newWordStart,-1);
						leadingCharPos = editor.doc.GoToEndOfWhitespace(leadingCharPos,-1);
						char leadingChar = editor.doc.GetCharAt(leadingCharPos);
						if (leadingChar == '=') {
							Vector2 wordStartPos = editor.doc.IncrementPosition(leadingCharPos,-1);
							wordStartPos = editor.doc.GoToEndOfWhitespace(wordStartPos,-1);
							string str = "";
							UIDEElement firstNonWhitespaceElement = line.GetFirstNonWhitespaceElement();
							if (editor.extension == ".js" && firstNonWhitespaceElement != null && firstNonWhitespaceElement.rawText == "var") {
								Vector2 typeStart = editor.doc.GoToNextRealChar(wordStartPos,':',-1);
								if (typeStart.y == wordStartPos.y) {
									typeStart = editor.doc.IncrementPosition(typeStart,1);
									typeStart = editor.doc.GoToEndOfWhitespace(typeStart,1);
									
									if (typeStart.x < wordStartPos.x) {
										str = line.rawText.Substring((int)typeStart.x,(int)wordStartPos.x-(int)typeStart.x+1);
										str = str.Replace(" ","");
										str = str.Replace("\t","");
										str = str.Replace(".<","<");
									}
								}
							}
							else {
								str = editor.syntaxRule.ResolveExpressionAt(wordStartPos,-1);
							}
							ChainResolver sigChainResolver = new ChainResolver(editor,editor.cursor.GetVectorPosition());
							
							ChainItem item = null;
							item = sigChainResolver.ResolveChain(str,false);
							
							if (item != null && item.finalLinkType != null) {
								CompletionItem cItem = new CompletionItem(item.finalLinkType);
								if (cItem != null) {
									string[] usingNamespaces = editor.syntaxRule.GetNamespacesVisibleInCurrentScope(editor.cursor.GetVectorPosition());
									string[] chainNamespaces = editor.syntaxRule.GetNamespaceChain(editor.cursor.GetVectorPosition());
									cItem.name = cItem.PrettyFormatType(false,usingNamespaces,chainNamespaces);
									//Debug.Log(cItem.genericArguments[0].resultingType);
									itemList = new List<CompletionItem>();
									itemList.Add(cItem);
									selectedIndex = 0;
									ShowBox();
								}
							}
							
							return;
						}
						
					}
				}
			}
			
			if (element != null) {
				isDot = element.tokenDef.HasType("Dot");
				isWord = element.tokenDef.HasType("Word");
				isWhiteSpace = element.tokenDef.HasType("WhiteSpace");
				bool isDotOrWord = isDot||isWord;
				
				autoCompleteKey = element.rawText;
				int elementPos = line.GetElementStartPos(element);
				bool isChain = false;
				
				if (isDot) {
					isChain = true;
				}
				else {
					if (elementPos > 0 && isWord) {
						UIDEElement previousElement = line.GetElementAt(elementPos-1);
						if (previousElement != null) {
							if (previousElement.tokenDef.HasType("Dot")) {
								isChain = true;
							}
						}
					}
				}
				
				if (genericMode) {
					isChain = false;
				}
				
				//bool newCharIsWhitespace = text == " "||text == "\t";
				
				if (visible && isWord) {
					//continue an existing autocomplete
				}
				if (!visible && isDotOrWord && !isBackspace) {
					TryStartUpdateAutoCompleteList(isChain);
				}
				if (visible && !isDotOrWord) {
					HideBox();
				}
				if (visible && isBackspace && !isDotOrWord) {
					HideBox();
				}
				//For performance on OSX.
				if (Application.platform == RuntimePlatform.OSXEditor) {
					if (visible && isBackspace) {
						HideBox();
						visible = false;
					}
				}
				if (visible && autoCompleteKey != "") {
					TryStartUpdateAutoCompleteList(isChain);
				}
				if (visible && isDot) {
					UpdateRect();
				}
				//TryStartUpdateAutoCompleteList();
			}
			else {
				if (visible) {
					HideBox();
				}
			}
			
			editor.editorWindow.Repaint();
	
		}
		
		public UIDEElement GetCursorElement() {
			return GetCursorElement(-1);
		}
		public UIDEElement GetCursorElement(int offset) {
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			if (line != null) {
				UIDEElement element = line.GetElementAt(editor.cursor.posX-1);
				return element;
			}
			return null;
		}
		
		public override void OnTextEditorGUI(int windowID) {
			if (!isEnabled) return;
			//StartShowTooltip(editor.ScreenSpaceToCursorSpace(editor.windowMousePos));
			GUI.color = new Color(1,1,1,1);
			
			if (!editor.TestClickBlockers(editor.windowMousePos,this)) {
				GUI.BringWindowToFront(windowID);
				if (Event.current.type == EventType.MouseDown) {
					GUI.FocusWindow(windowID);
				}
			}
			
			if (visible) {
				Draw();
			}
			if (showTooltip) {
				if (wantsTooltipRectUpdate) {
					UpdateToolTipRect();
				}
				if (tooltipText == "") {
					HideToolTip();
				}
				else {
					DrawToolTip();
				}
			}
		}
		
		public void UpdateToolTipRect() {
			UIDELine line = editor.doc.LineAt((int)toolTipPos.y);
			if (line == null) return;
			
			GUIStyle tooltipStyle = skin.GetStyle("ToolTip");
			
			toolTipMaxWidth = editor.textEditorNoScrollBarRectZeroPos.width-100.0f;
			toolTipMaxWidth = Mathf.Max(toolTipMaxWidth,100.0f);
			
			GUIContent content = new GUIContent(tooltipText);
			if (currentTooltip != null && currentTooltip.item != null) {
				Texture2D icon = editor.editorWindow.theme.GetLanguageIcon(currentTooltip.item.GetLanguageIconName());
				if (icon != null) {
					content.image = icon;
				}
			}
			
			//Create a tmp style to check the size without wordwrap.
			GUIStyle tooltipStyleTMP = new GUIStyle(tooltipStyle);
			tooltipStyleTMP.wordWrap = false;
			Vector2 clacedSize = tooltipStyleTMP.CalcSize(content);
			float desiredWidth = clacedSize.x;
			if (desiredWidth > toolTipMaxWidth) {
				desiredWidth = toolTipMaxWidth;
			}
			
			float height = tooltipStyle.CalcHeight(content, desiredWidth);
			
			tooltipRect.x = 0;
			tooltipRect.y = 0;
			tooltipRect.width = desiredWidth;
			tooltipRect.height = height;
			
			Vector2 screenCursorPos;
			
			screenCursorPos.x = line.GetScreenPosition((int)toolTipPos.x)*editor.charSize.x+editor.lineNumberWidth;
			screenCursorPos.y = toolTipPos.y*editor.charSize.y;
			
			screenCursorPos.x -= editor.doc.scroll.x;
			screenCursorPos.y -= editor.doc.scroll.y;
			
			tooltipRect.x = screenCursorPos.x-(desiredWidth*0.5f);
			tooltipRect.y = screenCursorPos.y-(height+editor.charSize.y*0.5f);
			
			float editorWidth = editor.textEditorNoScrollBarRectZeroPos.width;
			
			if (tooltipRect.x+tooltipRect.width > (editorWidth)) {
				float subtractFactor = (tooltipRect.x+tooltipRect.width)-(editorWidth);
				tooltipRect.x -= subtractFactor;
			}
			if (tooltipRect.x < editor.lineNumberWidth) {
				tooltipRect.x = editor.lineNumberWidth;
			}
			wantsTooltipRectUpdate = false;
		}
		
		public void UpdateRect() {
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			if (line == null) return;
			Vector2 screenCursorPos;
			
			screenCursorPos.x = line.GetScreenPosition(editor.cursor.posX)*editor.charSize.x+editor.lineNumberWidth;
			//screenCursorPos.x -= editor.doc.scroll.x;
			screenCursorPos.y = editor.cursor.posY*editor.charSize.y;
			//screenCursorPos -= editor.doc.scroll;
			float height = Mathf.Max(Mathf.Min(maxHeight,itemList.Count),1)*editor.charSize.y;
			rect = new Rect(screenCursorPos.x,screenCursorPos.y,boxWidth,height);
			
			float editorWidth = editor.textEditorNoScrollBarRectZeroPos.width;
			
			rect.y += editor.charSize.y;
			
			if (rect.x+rect.width > (editorWidth+editor.doc.scroll.x)-editor.lineNumberWidth) {
				float subtractFactor = (rect.x+rect.width)-((editorWidth+editor.doc.scroll.x));
				//Debug.Log((rect.x+rect.width)+" "+(editorWidth+editor.doc.scroll.x)+" "+subtractFactor);
				rect.x -= subtractFactor;
			}
			boxExpandedWidth = 0.0f;
		}
		
		public float GetItemHeight() {
			GUIStyle itemStyle = skin.GetStyle("ListItem");
			float itemHeight = itemStyle.CalcHeight(new GUIContent("#YOLO"),100.0f);
			return itemHeight;
		}
		
		private void DrawToolTip() {
			//GUIStyle bgStyle = skin.GetStyle("Background");
			GUIStyle tooltipStyle = skin.GetStyle("ToolTip");
			//GUIStyle itemSelectedStyle = skin.GetStyle("ItemSelected");
			GUIStyle shadowStyle = editor.editorWindow.skin.GetStyle("DropShadow");
				
			Rect clipRect = editor.textEditorNoScrollBarRect;
			clipRect.x += editor.rect.x;
			clipRect.y += editor.rect.y;
			GUI.BeginGroup(clipRect);
			
			if (shadowStyle != null) {
				GUI.Box(tooltipRect,"",shadowStyle);
			}
			
			TooltipItem tooltip = currentTooltip;
			
			GUIContent content = new GUIContent(tooltipText);
			Rect iconRect = new Rect(0,0,0,0);
			Texture2D icon = null;
			if (tooltip != null && tooltip.item != null) {
				icon = editor.editorWindow.theme.GetLanguageIcon(tooltip.item.GetLanguageIconName());
				if (icon != null) {
					content.image = Theme.invisible16x16Tex;
					iconRect.width = icon.width;
					iconRect.height = icon.height;
					float iconHeightDif = tooltipRect.height-iconRect.height;
					iconRect.x = tooltipRect.x;
					iconRect.y = tooltipRect.y;
					iconRect.y += iconHeightDif*0.5f;
					iconRect.x += tooltipStyle.padding.left;
				}
			}
			
			GUI.Box(tooltipRect,content,tooltipStyle);
			if (icon) {
				GUI.DrawTexture(iconRect,icon,ScaleMode.ScaleToFit,true);
			}
			
			GUI.EndGroup();
		}
		
		
		private void Draw() {
			
			GUIStyle bgStyle = skin.box;
			GUIStyle itemStyle = skin.GetStyle("ListItem");
			GUIStyle itemSelectedStyle = skin.GetStyle("ListItemSelected");
			GUIStyle shadowStyle = editor.editorWindow.skin.GetStyle("DropShadow");
			float itemHeight = GetItemHeight();
			
			Rect clipRect = editor.textEditorNoScrollBarRect;
			clipRect.x += editor.rect.x;
			clipRect.y += editor.rect.y;
			GUI.BeginGroup(clipRect);
			
			rect.width = Mathf.Max(boxWidth,boxExpandedWidth);
			rect.height = Mathf.Max(Mathf.Min(maxHeight,itemList.Count),1)*itemHeight;
			
			Rect boxRect = rect;
			boxRect.x -= editor.doc.scroll.x;
			boxRect.y -= editor.doc.scroll.y;
			Rect bgRect = boxRect;
			bgRect.x -= 1;
			bgRect.y -= 1;
			bgRect.width += 2;
			bgRect.height += 2;
			
			
			
			if (shadowStyle != null) {
				GUI.Box(bgRect,"",shadowStyle);
			}
			GUI.Box(bgRect,"",bgStyle);
			
			
			float darkColor = 1.0f;
			
			holdAutoCompleteUpdate = true;
			try {
				int startingLine = Mathf.Max(Mathf.FloorToInt(scroll.y/itemHeight),0);
				int endLine = startingLine+(Mathf.FloorToInt(boxRect.height/itemHeight)+1);
				endLine = (int)Mathf.Min(endLine,itemList.Count);
				
				//GUI.SetNextControlName("UIDEAutoCompleteScrollView");
				//GUI.FocusControl("UIDEAutoCompleteScrollView"); 
				Rect scrollContentRect = new Rect(0,0,boxRect.width-skin.verticalScrollbar.fixedWidth,itemList.Count*itemHeight);
				scroll = GUI.BeginScrollView(boxRect,scroll,scrollContentRect);
				
				bool hasScrollbar = false;
				float buttonWidth = boxRect.width;
				if (itemList.Count*itemHeight > boxRect.height) {
					buttonWidth -= skin.verticalScrollbar.fixedWidth;
					hasScrollbar = true;
				}
				
				for (int i = startingLine; i < endLine; i++) {
					CompletionItem item = itemList[i];
					bool isDark = (i % 2) == 0;
					if (isDark) {
						GUI.color = new Color(darkColor,darkColor,darkColor,1);
					}
					else {
						GUI.color = new Color(1,1,1,1);
					}
					
					Vector3 desiredSize = itemStyle.CalcSize(new GUIContent(itemList[i].displayName));
					float newBoxExpandedWidth = desiredSize.x;
					if (hasScrollbar) {
						newBoxExpandedWidth += skin.verticalScrollbar.fixedWidth;
					}
					boxExpandedWidth = newBoxExpandedWidth;
					//boxExpandedWidth = Mathf.Max(boxExpandedWidth,newBoxExpandedWidth);
					
					Rect buttonRect = new Rect(0,i*itemHeight, buttonWidth,itemHeight);
					GUIStyle iStyle = itemStyle;
					if (i == selectedIndex) {
						iStyle = itemSelectedStyle;
					}
					
					Texture2D icon = editor.editorWindow.theme.GetLanguageIcon(item.GetLanguageIconName());
					Rect iconRect = new Rect(0,0,0,0);
					if (icon != null) {
						iconRect.width = icon.width;
						iconRect.height = icon.height;
						float iconHeightDif = buttonRect.height-iconRect.height;
						iconRect.x = buttonRect.x;
						iconRect.y = buttonRect.y;
						iconRect.y += iconHeightDif*0.5f;
					}
					
					GUI.Box(buttonRect,itemList[i].displayName,iStyle);
					if (icon != null) {
						GUI.DrawTexture(iconRect,icon,ScaleMode.ScaleToFit,true);
					}
					
					if (UIDEGUI.TestClick(0,buttonRect)) {
						selectedIndex = i;
						if (Event.current.clickCount == 2) {
							OnSubmit();
						}
						Event.current.Use();
					}
				}
				GUI.color = Color.white;
				
				GUI.EndScrollView();
			}
			catch (Exception ex) {
				Debug.LogError(ex.Message);
			}
			holdAutoCompleteUpdate = false;
			
			GUI.EndGroup();
		}
		
		private bool StartShowTooltip(Vector2 pos, bool showMethodOverloads) {
			if (!isEnabled) return true;
			
			if (useMultiThreading) {
				if (UIDEThreadPool.IsRegistered("AutoComplete_UpdateTooltip")) {
					wantsTooltipUpdate = true;
					wantsTooltipUpdateOverloads = showMethodOverloads;
					return false;
				}
				toolTipPos = pos;
				wantsTooltipUpdate = false;
				
				UIDEThreadPool.RegisterThread("AutoComplete_UpdateTooltip",StartShowTooltipActual,new System.Object[] {toolTipPos,showMethodOverloads});
			}
			else {
				toolTipPos = pos;
				wantsTooltipUpdate = false;
				StartShowTooltipActual(new System.Object[] {toolTipPos,showMethodOverloads});
			}
			return true;
		}
		private void StartShowTooltipActual(System.Object context) {
			try {
				System.Object[] contextArray = (System.Object[])context;
				Vector2 pos = (Vector2)contextArray[0];
				bool showMethodOverloads = (bool)contextArray[1];
				
				if (showMethodOverloads) {
					CompletionMethod[] methods = editor.syntaxRule.GetMethodOverloads(pos);
					TooltipItem[] items = new TooltipItem[methods.Length];
					for (int i = 0; i < methods.Length; i++) {
						items[i] = new TooltipItem(methods[i]);
					}
					if (cancelTooltip) {
						HideToolTip();
					}
					else {
						ShowToolTip(items);
					}
				}
				else {
					TooltipItem tItem = editor.syntaxRule.GetTooltipItem(pos);
					if (cancelTooltip) {
						HideToolTip();
					}
					else {
						ShowToolTip(tItem);
					}
				}
				editor.editorWindow.Repaint();
				cancelTooltip = false;
			}
			finally {
				//UIDEThreadPool.UnregisterThread("AutoComplete_UpdateTooltip");
			}
		}
		
		private void OnFinishUpdateAutoCompleteListActual() {
			if (!visible && !newVisibleState) {
				if (itemList.Count > 0) {
					ShowBox();
				}
			}
			if (visible || newVisibleState) {
				if (itemList.Count == 0) {
					HideBox();
				}
			}
			scroll.y = 0.0f;
			wantsFunishedUpdateAutoComplete = false;
			editor.editorWindow.Repaint();
		}
		public bool TryStartUpdateAutoCompleteList(bool isChain) {
			if (!isEnabled) return true;
			if (useMultiThreading) {
				
				if (UIDEThreadPool.IsRegistered("AutoComplete_UpdateAutoCompleteList")) {
					wantsChainUpdate = isChain;
					wantsAutoCompleteUpdate = true;
					return false;
				}
				wantsAutoCompleteUpdate = false;
				UIDEThreadPool.RegisterThread("AutoComplete_UpdateAutoCompleteList",UpdateAutoCompleteList,isChain);
			}
			else {
				wantsAutoCompleteUpdate = false;
				UpdateAutoCompleteList(isChain);
			}
			return true;
		}
		
		void UpdateAutoCompleteList(System.Object context) {
			try {
				bool isChain = (bool)context;
				UpdateAutoCompleteListActual(isChain);
				//Debug.Log(UIDEThreadPool.IsRegistered("AutoComplete_UpdateAutoCompleteList")+" "+isChain);
			}
			finally {
				//UIDEThreadPool.UnregisterThread("AutoComplete_UpdateAutoCompleteList");
			}
			
		}
		void UpdateAutoCompleteListActual(bool isChain) {
			//float startTime = Time.realtimeSinceStartup;
			string startingAutoCompleteKey = autoCompleteKey;
			
			CompletionItem[] completionItems = new CompletionItem[0];
			if (startingAutoCompleteKey == ".") {
				startingAutoCompleteKey = "";
			}
			
			if (isChain) {
				completionItems = editor.syntaxRule.GetChainCompletionItems();
			}
			else {
				completionItems = editor.syntaxRule.GetGlobalCompletionItems();
				
			}
			
			//Debug.Log("AutoCompleteKey: "+startingAutoCompleteKey+" "+isChain);
			//Dictionary<string,,GameObject> bb;
			//bb = new Dictionary<string, UnityEngine.GameObject>
			List<UIDEFuzzySearchItem> inputItems = new List<UIDEFuzzySearchItem>();
			for (int i = 0; i < completionItems.Length; i++) {
				if (completionItems[i] == null) continue;
				if (completionItems[i].displayName.Length == 0) continue;
				char c = completionItems[i].displayName[0];
				if (!char.IsLetter(c) && c != '_') continue;
				
				if (completionItems[i].isType || completionItems[i].isClassOrStruct) {
					completionItems[i].name = completionItems[i].GetCompletionFriendlyName();
				}
				
				UIDEFuzzySearchItem fuzzyItem = new UIDEFuzzySearchItem();
				fuzzyItem.text = CleanStringForSearch(completionItems[i].displayName);
				fuzzyItem.metaObject = completionItems[i];
				inputItems.Add(fuzzyItem);
			}
			
			UIDEFuzzySearchItem[] sortedFuzzyItems = UIDEFuzzySearch.GetSortedList(startingAutoCompleteKey,inputItems.ToArray(),false,false);
			
			List<CompletionItem> sortedCompletionItems = new List<CompletionItem>();
			
			//Type sorting
			CompletionItem lastMetaItem = null;
			UIDEFuzzySearchItem lastFuzzyItem = null;
			for (int i = 0; i < sortedFuzzyItems.Length; i++) {
				CompletionItem metaItem = (CompletionItem)sortedFuzzyItems[i].metaObject;
				if (lastFuzzyItem != null && lastFuzzyItem.score == sortedFuzzyItems[i].score) {
					int compareValue = metaItem.Compare(lastMetaItem);
					if (compareValue == 1) {
						sortedCompletionItems.Insert(i-1,metaItem);
					}
					else {
						sortedCompletionItems.Add(metaItem);
					}
				}
				else {
					sortedCompletionItems.Add(metaItem);
				}
				lastMetaItem = metaItem;
				lastFuzzyItem = sortedFuzzyItems[i];
			}
			
			int bestIndex = 0;
			UIDEHashTable hashTable = new UIDEHashTable();
			List<CompletionItem> acItems = new List<CompletionItem>();
			for (int i = 0; i < sortedCompletionItems.Count; i++) {
				//CompletionItem newItem = new CompletionItem();
				//newItem.fuzzyItem = items[i];
				
				CompletionItem existingItem = (CompletionItem)hashTable.Get(sortedCompletionItems[i].displayName);
				//if (existingItem != null) {
				//	existingItem.others.Add(newItem);
				//}
				//else {
				if (existingItem == null) {
					hashTable.Set(sortedCompletionItems[i].displayName,sortedCompletionItems[i]);
					acItems.Add(sortedCompletionItems[i]);
				}
				//selectedText
			}
			
			if (acItems.Count <= maxHeight) {
				List<CompletionItem> newACItems = new List<CompletionItem>();
				for (int i = 0; i < acItems.Count; i++) {
					bool isInsert = (i % 2) == 0;
					if (isInsert) {
						bestIndex++;
						newACItems.Insert(0,acItems[i]);
					}
					else {
						newACItems.Add(acItems[i]);
					}
				}
				acItems = newACItems;
				bestIndex = Mathf.Max(bestIndex-1,0);
			}
			
			while (holdAutoCompleteUpdate) {
				Thread.Sleep(1);
			}
			
			itemList = acItems;
			
			selectedIndex = bestIndex;
			wantsFunishedUpdateAutoComplete = true;
	
			editor.editorWindow.Repaint();
			//Debug.Log(Time.realtimeSinceStartup-startTime);
		}
		
		private static string CleanStringForSearch(string str){
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for (int i = 0; i < str.Length; i++){
				char c = str[i];
				switch (c){
					case '<':
					case ',':
					case '>':
						continue;
					default:
						sb.Append(c);
					break;
				}
			}
			return sb.ToString();
		}
		
		static string GetRootNamespace(string ns){
		    int firstDot = ns.IndexOf('.');
		    return firstDot == -1 ? ns : ns.Substring(0, firstDot);
		}
		
		static string GetParentNamespace(string ns){
		    int lastDot = ns.LastIndexOf('.');
			if (lastDot == -1) {
				return ns;
			}
			ns = ns.Substring(lastDot+1);
		    
			lastDot = ns.LastIndexOf('.');
			if (lastDot == -1) {
				return ns;
			}
			ns = ns.Substring(lastDot+1);
			return ns;
		}
		
		static string GetLeafNamespace(string ns){
		    int lastDot = ns.LastIndexOf('.');
		    return lastDot == -1 ? ns : ns.Substring(lastDot+1);
		}
		
		static string[] GetNamespaceParts(string namespaceName){
			//int firstDot = namespaceName.IndexOf('.');
			return namespaceName.Split("."[0]);
			//return firstDot == -1 ? namespaceName : namespaceName.Substring(0, firstDot);
		}
	}
}