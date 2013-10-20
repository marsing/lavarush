using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Reflection;

using UIDE.SyntaxRules.ExpressionResolvers.CSharp;
using UIDE.SyntaxRules.ExpressionResolvers;
using UIDE.SyntaxRules;
using UIDE.CodeCompletion.Parsing;

//using UIDE.CodeCompletion.CSharp;
using UIDE.CodeCompletion;

namespace UIDE.SyntaxRules.Shared {
	[System.Serializable]
	public class SyntaxRuleCSharpUnityscript:SyntaxRule {
		public float lastReparseTime = 0.0f;
		public bool conservativeParsing = false;
		private bool wantsMultiLineFormattingUpdate = false;
		
		private bool wantsParserUpdate = false;
		private bool wantsChainResolverUpdate = false;
		
		private bool useUnityscript = false;
		
		private ChainResolver chainResolver;
		private bool isCreatingChainResolver = false;
		
		private ParserInterface _parserInterface;
		public ParserInterface parserInterface {
			get {
				if (_parserInterface == null) {
					_parserInterface = new ParserInterface();
				}
				return _parserInterface;
			}
		}
		
		public SyntaxRuleCSharpUnityscript() {
			fileTypes = new string[] {".cs",".js"};
		}
		
		public override void OnTextEditorUpdate() {
			if (wantsParserUpdate && editor.editorWindow.time-lastReparseTime > 1.0f) {
				if (Reparse()) {
					wantsParserUpdate = false;
				}
			}
			if (wantsChainResolverUpdate) {
				if (UpdateChainResolver()) {
					wantsChainResolverUpdate = false;
				}
			}
			if (wantsMultiLineFormattingUpdate) {
				if (UpdateMultilineFormatting()) {
					//wantsMultiLineFormattingUpdate = false;
				} 
			}
		}
		
		public override void Start() {
			UIDEThreadPool.timeout = 5.0f;
			lastReparseTime = editor.editorWindow.time;
			useUnityscript = editor.extension == ".js";
			
			conservativeParsing = Application.platform == RuntimePlatform.OSXEditor;
			conservativeParsing |= true;
			
			chainResolver = null;
			Reparse();
			UpdateChainResolver();
		}
		
		public override void OnFocus() {
			//chainResolver = null;
			Reparse();
			UpdateChainResolver();
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnSwitchToTab() {
			//chainResolver = null;
			Reparse();
			UpdateChainResolver();
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnRebuildLines(UIDEDoc doc) {
			base.OnRebuildLines(doc);
			//chainResolver = null;
			//Reparse();
			//UpdateChainResolver();
		}
		
		public override void OnPostBackspace() {
			if (!conservativeParsing) {
				Reparse();
				UpdateChainResolver();
			}
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnClickMoveCursor(Vector2 pos) {
			UpdateChainResolver();
			if (conservativeParsing && editor.lastModifyTime > lastReparseTime) {
				Reparse();
			}
		}
		
		public override void OnArrowKeyMoveCursor(Vector2 pos) {
			UIDELine line = editor.doc.RealLineAt((int)pos.y);
			if (line == null) return;
			UIDEElement element = line.GetElementAt((int)pos.x);
			if (element == null) return;
			if (element.tokenDef.isActualCode == true) {
				char c = (char)0;
				if (pos.x < line.rawText.Length) {
					c = line.rawText[(int)pos.x];
				}
				if (c == ';' || c == '{' || c == '}') {
					UpdateChainResolver();
				}
				
			}
			if (conservativeParsing && editor.lastModifyTime > lastReparseTime) {
				Reparse();
			}
		}
		public override void OnArrowKeyMoveCursorLine(Vector2 pos) {
			UpdateChainResolver();
			if (conservativeParsing && editor.lastModifyTime > lastReparseTime) {
				Reparse();
			}
		}
		
		public override void OnPostEnterText(string text) {
			if (text == "\r" || text == "\n") {
				OnNewLine();
			}
			if (text == ":") {
				OnColon();
			}
			if (conservativeParsing) {
				if (text == ";" || text == "=" || text == "\r" || text == "\n" || text == "{" || text == "}") {
					Reparse();
					UpdateChainResolver();
				}
			}
			else {
				Reparse();
				UpdateChainResolver();
			}
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override string OnPreEnterText(string text) {
			if (text == "}") {
				OnCloseCurly();
			}
			return text;
		}
		
		public void OnCloseCurly() {
			if (editor.cursor.posY <= 0) {
				return;
			}
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			UIDELine previousLine = editor.doc.GetLastNoneWhitespaceOrCommentLine(editor.cursor.posY-1);
			if (previousLine == line) {
				return;
			}
			
			if (!line.IsLineWhitespace()) {
				return;
			}
			
			UIDEElement firstElement = previousLine.GetFirstNonWhitespaceElement();
			int previousLineStartPos = previousLine.GetElementStartPos(firstElement);
			int screenPos = previousLine.GetScreenPosition(previousLineStartPos);
			UIDEElement lastElement = previousLine.GetLastNonWhitespaceElement();
			
			int tabCount = screenPos/4;
			if (lastElement != null) {
				if (lastElement.tokenDef.HasType("LineEnd")) {
					tabCount -= 1;
				}
			}
			tabCount = Mathf.Max(tabCount,0);
			line.rawText = line.GetTrimmedWhitespaceText();
			for (int i = 0; i < tabCount; i++) {
				line.rawText = "\t"+line.rawText;
			}
			
			//line.rawText += startingText;
			line.RebuildElements();
			editor.cursor.posX = tabCount;
		}
		public void OnColon() {
			UIDELine line = editor.doc.RealLineAt((int)editor.cursor.posY);
			if (line == null) return;
			if (editor.cursor.posY < line.rawText.Length) return;
			UIDEElement firstElement = line.GetFirstNonWhitespaceElement();
			if (firstElement == null) return;
			
			if (firstElement.tokenDef.HasType("Word") && firstElement.rawText == "case") {
				if (line.rawText[0] == '\t') {
					//string originalText = lines[i].rawText;
					line.rawText = line.rawText.Substring(1);
					line.RebuildElements();
				}
				else if (line.rawText[0] == ' ') {
					int tabSize = editor.editorWindow.textSettings.GetTabSize();
					//string originalText = lines[i].rawText;
					for (int j = 0; j < tabSize; j++) {
						if (line.rawText.Length <= 0 || line.rawText[0] != ' ') break;
						line.rawText = line.rawText.Substring(1);
					}
					line.RebuildElements();
				}
				editor.cursor.posX = line.rawText.Length;
			}
		}
		public void OnNewLine() {
			if (editor.cursor.posY <= 0) {
				return;
			}
			
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			UIDELine previousLine = editor.doc.GetLastNoneWhitespaceOrCommentLine(editor.cursor.posY-1);
			if (previousLine == null) return;
			UIDEElement firstElement = previousLine.GetFirstNonWhitespaceElement();
			int previousLineStartPos = previousLine.GetElementStartPos(firstElement);
			int screenPos = previousLine.GetScreenPosition(previousLineStartPos);
			UIDEElement lastElement = previousLine.GetLastNonWhitespaceElement();
			
			string originalText = line.rawText;
			int tabCount = screenPos/4;
			if (lastElement != null && (lastElement.rawText == "{" || lastElement.rawText == ":")) {
				tabCount += 1;
			}
			line.rawText = line.GetTrimmedWhitespaceText();
			for (int i = 0; i < tabCount; i++) {
				line.rawText = "\t"+line.rawText;
			}
			line.RebuildElements();
			
			Vector2 oldCursorPos = editor.cursor.GetVectorPosition();
			editor.cursor.posX = tabCount;
			Vector2 newCursorPos = editor.cursor.GetVectorPosition();
			//add another undo with the same name as the previous one so it gets grouped.
			if (editor.undoManager.undos.Count > 0) {
				string undoName = editor.undoManager.undos[editor.undoManager.undos.Count-1].groupID;
				editor.undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
			}
		}
		
		public override bool CheckIfStringIsKeyword(string str) {
			if (useUnityscript) {
				return !(UIDE.SyntaxRules.Unityscript.Keywords.keywordHash.Get(str) == null);
			}
			return !(UIDE.SyntaxRules.CSharp.Keywords.keywordHash.Get(str) == null);
		}
		public override bool CheckIfStringIsModifier(string str) {
			if (useUnityscript) {
					return !(UIDE.SyntaxRules.Unityscript.Keywords.modifierHash.Get(str) == null);
			}
			return !(UIDE.SyntaxRules.CSharp.Keywords.modifierHash.Get(str) == null);
		}
		public override bool CheckIfStringIsPrimitiveType(string str) {
			if (useUnityscript) {
					return !(UIDE.SyntaxRules.Unityscript.Keywords.primitiveTypeHash.Get(str) == null);
			}
			return !(UIDE.SyntaxRules.CSharp.Keywords.primitiveTypeHash.Get(str) == null);
		}
		public override UIDETokenDef GetKeywordTokenDef(UIDETokenDef tokenDef, string str) {
			if (CheckIfStringIsKeyword(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,Keyword");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			else if (CheckIfStringIsModifier(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,Modifier");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			else if (CheckIfStringIsPrimitiveType(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,PrimitiveType");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			if (APITokens.IsTypeKeyword(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,APIToken,Type");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			return tokenDef;
		}
		
		public override string ResolveExpressionAt(Vector2 position, int dir) {
			ExpressionResolver.editor = editor;
			return ExpressionResolver.ResolveExpressionAt(position,dir);
		}
		
		
		private bool Reparse() {
			if (editor.editorWindow.generalSettings.GetDisableCompletion()) {
				return true;
			}
			if (editor.editorWindow.generalSettings.GetForceGenericAutoComplete()) {
				return true;
			}
			
			if (useMultiThreading) {
				if (UIDEThreadPool.IsRegistered("SRCSUS_Reparse") || editor.editorWindow.time-lastReparseTime <= 1.0f) {
					wantsParserUpdate = true;
					return false;
				}
				lastReparseTime = editor.editorWindow.time;
				wantsParserUpdate = false;
				UIDEThreadPool.RegisterThread("SRCSUS_Reparse",ReparseActual);
			}
			else {
				wantsParserUpdate = false;
				ReparseActual(null);
			}
			
			return true;
		}
		
		private void ReparseActual(System.Object context) {
			try {
				lastReparseTime = editor.editorWindow.time;
				//Debug.Log("Reparse");
				string text = editor.doc.GetParsableText();
				if (useUnityscript) {
					parserInterface.Reparse(this,text,"us");
				}
				else {
					parserInterface.Reparse(this,text,"cs");
				}
			}
			finally {
				//UIDEThreadPool.UnregisterThread("SRCSUS_Reparse");
			}
			lastReparseTime = editor.editorWindow.time;
		}
		
		private void VerifyChainResolver() {
			if (chainResolver == null) {
				if (isCreatingChainResolver) {
					while (isCreatingChainResolver) {
						//Thread.Sleep(10);
					}
				}
				else {
					UpdateChainResolver();
				}
			}
		}
		
		private bool UpdateChainResolver() {
			if (editor.editorWindow.generalSettings.GetDisableCompletion()) {
				return true;
			}
			if (editor.editorWindow.generalSettings.GetForceGenericAutoComplete()) {
				return true;
			}
			if (useMultiThreading) {
				if (UIDEThreadPool.IsRegistered("SRCSUS_UpdateChainResolver")) {
					wantsChainResolverUpdate = true;
					return false;
				}
				wantsChainResolverUpdate = false;
				UIDEThreadPool.RegisterThread("SRCSUS_UpdateChainResolver",UpdateChainResolverActual);
			}
			else {
				wantsChainResolverUpdate = false;
				UpdateChainResolverActual(null);
			}
			
			return true;
		}
		private void UpdateChainResolverActual(System.Object context) {
			isCreatingChainResolver = true;
			try {
				try {
					//Debug.Log("UpdateChainResolverActual");
					if (chainResolver != null && chainResolver.reflectionDB != null && !chainResolver.reflectionDB.needsToBeKilled) {
						chainResolver.Refresh(editor,editor.cursor.GetVectorPosition());
					}
					else {
						//Debug.Log("UpdateChainResolverActual");
						chainResolver = new ChainResolver(editor,editor.cursor.GetVectorPosition());
					}
				}
				catch (System.Exception ex) {
					Debug.LogError(ex.Message);
				}
			}
			finally {
				//UIDEThreadPool.UnregisterThread("SRCSUS_UpdateChainResolver");
			}
			isCreatingChainResolver = false;
			
		}
		
		
		
		public override CompletionMethod[] GetMethodOverloads(Vector2 pos) {
			//Vector2 originalPos = pos;
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			pos = editor.doc.IncrementPosition(pos,-1);
			pos = editor.doc.IncrementPosition(pos,-1);
			pos = editor.doc.GoToEndOfWhitespace(pos,-1);
			
			//char nextChar = editor.doc.GetCharAt(pos);
			if (editor.doc.GetCharAt(pos) == '>') {
				ExpressionResolver.editor = editor;
				pos = ExpressionResolver.SimpleMoveToEndOfScope(pos,-1,ExpressionBracketType.Generic);
				pos = editor.doc.GoToEndOfWhitespace(pos,-1);
				if (useUnityscript) {
					if (editor.doc.GetCharAt(pos) == '.') {
						pos = editor.doc.IncrementPosition(pos,-1);
					}
				}
				pos = editor.doc.GoToEndOfWhitespace(pos,-1);
				//GameObject go;
				//go.GetComponent<Vector3>();
			}
			Vector2 endWordPos = pos;
			
			pos = editor.doc.GoToEndOfWord(pos,-1);
			Vector2 startWordPos = editor.doc.IncrementPosition(pos,1);
			pos = editor.doc.GoToEndOfWhitespace(pos,-1);
			//
			
			//Debug.Log(editor.doc.GetCharAt(pos));
			bool hasDot = false;
			if (editor.doc.GetCharAt(pos) == '.') {
				if (useUnityscript) {
					if (editor.doc.GetCharAt(editor.doc.IncrementPosition(pos,1)) != '<') {
						hasDot = true;
					}
				}
				else {
					hasDot = true;
				}
			}
			
			UIDELine startLine = editor.doc.RealLineAt((int)startWordPos.y);
			string functionName = startLine.rawText.Substring((int)startWordPos.x,((int)endWordPos.x-(int)startWordPos.x)+1);
			
			pos = editor.doc.IncrementPosition(pos,-1);
			pos = editor.doc.GoToEndOfWhitespace(pos,-1);
			
			string str = editor.syntaxRule.ResolveExpressionAt(pos,-1);
			if (useUnityscript) {
				str = str.Replace(".<","<");
			}
			//Debug.Log(str);
			
			CompletionMethod[] methods = new CompletionMethod[0];
			ChainResolver sigChainResolver = new ChainResolver(editor,pos);
			
			//Handle constructors
			bool isDirectConstructor = str == "new|";
			bool isIndirectConstructor = !isDirectConstructor && str.StartsWith("new|");
			if (isIndirectConstructor && hasDot) {
				isIndirectConstructor = false;
			}
			if (isIndirectConstructor) {
				ChainItem item = null;
				item = sigChainResolver.ResolveChain(str+"."+functionName);
				if (item == null || item.finalLinkType == null) {
					return methods;
				}
				methods = sigChainResolver.GetConstructors(item.finalLinkType);
				return methods;
			}
			else if (isDirectConstructor) {
				ChainItem item = null;
				item = sigChainResolver.ResolveChain(functionName);
				if (item == null || item.finalLinkType == null) {
					return methods;
				}
				methods = sigChainResolver.GetConstructors(item.finalLinkType);
				return methods;
			}
			
			System.Type type = sigChainResolver.reflectionDB.currentType;
			bool isStatic = false;
			if (hasDot) {
				ChainItem item = null;
				item = sigChainResolver.ResolveChain(str,false);
				if (item == null || item.finalLinkType == null) {
					return methods;
				}
				isStatic = item.finalLink.isStatic;
				type = item.finalLinkType;
			}
			
			methods = sigChainResolver.GetMethodOverloads(type,functionName,isStatic);
			
			return methods;
		}
		
		public override TooltipItem GetTooltipItem(Vector2 pos) {
			Vector2 originalPos = pos;
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			
			pos = editor.doc.GoToEndOfWord(pos,1);
			Vector2 endWordPos = editor.doc.IncrementPosition(pos,-1);
			pos = editor.doc.GoToEndOfWhitespace(pos,1);
			
			ExpressionInfo result = new ExpressionInfo();
			result.startPosition = pos;
			result.endPosition = pos;
			result.initialized = true;
			
			char nextChar = editor.doc.GetCharAt(pos);
			bool isBeginGeneric = nextChar == '<';
			if (useUnityscript) {
				Vector2 charAfterPos = editor.doc.GoToEndOfWhitespace(editor.doc.IncrementPosition(pos,1),1);
				char charAfter = editor.doc.GetCharAt(charAfterPos);
				if (nextChar == '.' && charAfter == '<') {
					pos = charAfterPos;
					nextChar = editor.doc.GetCharAt(pos);
					isBeginGeneric = true;
				}
			}
			//GameObject go;
			
			//go.GetComponent<Vector3>();
			if (isBeginGeneric) {
				result.startPosition = pos;
				result.endPosition = pos;
				ExpressionResolver.editor = editor;
				result = ExpressionResolver.CountToExpressionEnd(result,1,ExpressionBracketType.Generic);
				
				pos = result.endPosition;
				pos = editor.doc.IncrementPosition(pos,1);
				pos = editor.doc.GoToEndOfWhitespace(pos,1);
				
				result.startPosition = pos;
				result.endPosition = pos;
				nextChar = editor.doc.GetCharAt(pos);
			}
			
			bool isFunction = false;
			if (nextChar == '(') {
				ExpressionResolver.editor = editor;
				result = ExpressionResolver.CountToExpressionEnd(result,1,ExpressionBracketType.Expression);
				pos = result.endPosition;
				nextChar = editor.doc.GetCharAt(pos);
				isFunction = true;
			}
			
			if (!isFunction) {
				pos = endWordPos;
			}
			
			//Debug.Log(nextChar+" "+editor.doc.GetCharAt(endWordPos));
			
			string str = editor.syntaxRule.ResolveExpressionAt(pos,-1);
			if (useUnityscript) {
				str = str.Replace(".<","<");
			}
			//Debug.Log(str);
			
			ChainResolver sigChainResolver = new ChainResolver(editor,originalPos);
			ChainItem item = null;
			item = sigChainResolver.ResolveChain(str,false);
			
			TooltipItem tooltipItem = null;
			if (item != null) {
				if (item.finalLinkType != null) {
					tooltipItem = new TooltipItem(item.finalLinkType.Name+" "+item.finalLink.name);
					tooltipItem.clrType = item.finalLinkType;
				}
				
				if (item.finalLink.completionItem != null) {
					tooltipItem = new TooltipItem(item.finalLink.completionItem);
				}
				
			}
			
			return tooltipItem;
		}
		
		public override CompletionItem[] GetChainCompletionItems() {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			if (conservativeParsing && isCreatingChainResolver) {
				int i = 0;
				while(isCreatingChainResolver && i < 100) {
					Thread.Sleep(10);
					i++;
				}
			}
			
			if (conservativeParsing) {
				UpdateChainResolverActual(null);
			}
			
			List<CompletionItem> items = new List<CompletionItem>();
			
			Vector2 previousCharPos = editor.cursor.GetVectorPosition();
			previousCharPos = editor.doc.IncrementPosition(previousCharPos,-1);
			
			UIDELine line = editor.doc.RealLineAt((int)previousCharPos.y);
			UIDEElement element = line.GetElementAt((int)previousCharPos.x);
			
			Vector2 expressionStartPos = previousCharPos;
			
			bool lastCharIsDot = element.tokenDef.HasType("Dot");
			if (lastCharIsDot) {
				expressionStartPos = editor.doc.IncrementPosition(expressionStartPos,-1);
			}
			else {
				int elementPos = line.GetElementStartPos(element);
				if (elementPos >= 2) {
					
					element = line.GetElementAt(elementPos-1);
					if (element.tokenDef.HasType("Dot")) {
						expressionStartPos.x = elementPos-2;
					}
				}
			}
			
			ChainItem item = null;
			string str = ResolveExpressionAt(expressionStartPos,-1);
			if (useUnityscript) {
				str = str.Replace(".<","<");
			}
			//Debug.Log(str);
			
			VerifyChainResolver();
			
			item = chainResolver.ResolveChain(str);
			
			if (item != null) {
				items = item.autoCompleteItems;
			}
			
			return items.ToArray();
		}
		
		public override CompletionItem[] GetGlobalCompletionItems() {
			if (editor.editorWindow.generalSettings.GetForceGenericAutoComplete()) {
				return GetGenericCompletionItems();
			}
			
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			if (conservativeParsing && parserInterface != null && parserInterface.isParsing) {
				int i = 0;
				while(parserInterface.isParsing && i < 100) {
					Thread.Sleep(10);
					i++;
				}
			}
			
			//float startTime = Time.realtimeSinceStartup;
			List<CompletionItem> items = new List<CompletionItem>();
			
			//items = parserInterface.GetCurrentVisibleItems(editor.cursor.GetVectorPosition(), this).ToList();
			//Debug.Log(Time.realtimeSinceStartup-startTime);
			string[] keywords = UIDE.SyntaxRules.CSharp.Keywords.keywords;
			string[] modifiers = UIDE.SyntaxRules.CSharp.Keywords.modifiers;
			string[] primitiveTypes = UIDE.SyntaxRules.CSharp.Keywords.primitiveTypes;
			if (useUnityscript) {
				keywords = UIDE.SyntaxRules.Unityscript.Keywords.keywords;
				modifiers = UIDE.SyntaxRules.Unityscript.Keywords.modifiers;
				primitiveTypes = UIDE.SyntaxRules.Unityscript.Keywords.primitiveTypes;
			}
			for (int i = 0; i < keywords.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromKeyword(keywords[i]);
				items.Add(item);
			}
			for (int i = 0; i < modifiers.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromModifier(modifiers[i]);
				items.Add(item);
			}
			for (int i = 0; i < primitiveTypes.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromPrimitiveType(primitiveTypes[i]);
				items.Add(item);
			}
			
			//Add members of the current type
			string typeName = "new|"+GetCurrentTypeFullName(editor.cursor.GetVectorPosition())+"()";
			ChainItem typeItem = null;
			
			VerifyChainResolver();
			
			CompletionItem[] globalItems = chainResolver.GetCurrentlyVisibleGlobalItems();
			items.AddRange(globalItems);
			
			items.AddRange(parserInterface.GetCurrentVisibleItems(editor.cursor.GetVectorPosition(), this));
			
			typeItem = chainResolver.ResolveChain(typeName);
			if (typeItem != null) {
				items.AddRange(typeItem.autoCompleteItems);
			}
			
			string[] interfaces = GetCurrentTypeInterfaceNames(editor.cursor.GetVectorPosition());
			
			for (int i = 0; i < interfaces.Length; i++) {
				typeItem = null;
				typeItem = chainResolver.ResolveChain(interfaces[i]);
				if (typeItem != null) {
					items.AddRange(typeItem.autoCompleteItems);
				}
			}
			
			return items.ToArray();
		}
		
		public override string[] GetNamespacesVisibleInCurrentScope(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string[] namespaces = parserInterface.GetNamespacesVisibleInCurrentScope(pos,this);
			return namespaces;
		}
		public override string[] GetNamespaceChain(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string[] namespaces = parserInterface.GetNamespaceChain(pos,this);
			return namespaces;
		}
		public override string[] GetAllVisibleNamespaces(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			List<string> ns = new List<string>();
			ns.AddRange(parserInterface.GetNamespacesVisibleInCurrentScope(pos,this));
			ns.AddRange(parserInterface.GetNamespaceChain(pos,this));
			return ns.ToArray();
		}
		
		public override string GetCurrentTypeFullName(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string typeName = parserInterface.GetCurrentTypeFullName(pos,this);
			return typeName;
		}
		public override string GetCurrentTypeNestedTypePath(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string typeName = parserInterface.GetCurrentTypeNestedTypePath(pos,this);
			return typeName;
		}
		public override string GetCurrentTypeNamespace(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string typeName = parserInterface.GetCurrentTypeNamespace(pos,this);
			return typeName;
		}
		public override string GetCurrentTypeBaseTypeFullName(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string typeName = parserInterface.GetCurrentTypeBaseTypeFullName(pos,this);
			return typeName;
		}
		public override string[] GetCurrentTypeInterfaceNames(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			string[] typeName = parserInterface.GetCurrentTypeInterfaceNames(pos,this);
			return typeName;
		}
		
		public override CompletionItem[] GetCurrentVisibleItems(Vector2 pos) {
			if (parserInterface.lastSourceFile == null) {
				Reparse();
			}
			
			CompletionItem[] items = parserInterface.GetCurrentVisibleItems(pos,this);
			return items;
		}
		
		public override bool CheckIfShouldLoad(UIDETextEditor textEditor) {
			return HasFileType(textEditor.extension);
		}
		
		public override void OnRebuildLineElements(UIDELine line) {
			base.OnRebuildLineElements(line);
			
			wantsMultiLineFormattingUpdate = true;
		}
		
		public bool UpdateMultilineFormatting() {
			
			if (useMultiThreading) {
				if (UIDEThreadPool.IsRegistered("SRCSUS_UpdateMultilineFormatting")) {
					wantsMultiLineFormattingUpdate = true;
					return false;
				}
				wantsMultiLineFormattingUpdate = false;
				UIDEThreadPool.RegisterThread("SRCSUS_UpdateMultilineFormatting",UpdateMultilineFormattingActual);
				
			}
			else {
				wantsMultiLineFormattingUpdate = false;
				UpdateMultilineFormattingActual(null);
			}
			return true;
		}
		
		private void UpdateMultilineFormattingActual(System.Object context) {
			try {
				UpdateMultilineFormattingGeneric();
				
				if (editor.editorWindow.generalSettings.GetUseCodeFolding() && parserInterface.lastSourceFile != null) {
					//lock (parserInterface) {
						bool[] newLineIsFoldable = new bool[editor.doc.lineCount];
						int[] newLineFoldingLength = new int[editor.doc.lineCount];
						ReparseActual(null);
						List<StatementBlock> blocks = GetStatementBlocksRecursive(parserInterface.lastSourceFile.statementBlock);
						
						foreach (StatementBlock block in blocks) {
							int startLine = block.startLine;
							int endLine = block.endLine;
							if (startLine >= endLine) continue;
							int foldingLength = endLine-startLine;
							UIDELine line = editor.doc.RealLineAt(startLine);
							if (line == null) continue;
							newLineIsFoldable[line.index] = true;
							newLineFoldingLength[line.index] = foldingLength;
						}
						
						for (int i = 0; i < newLineIsFoldable.Length; i++) {
							UIDELine line = editor.doc.RealLineAt(i);
							if (line == null) continue;
							line.isFoldable = newLineIsFoldable[i];
							line.foldingLength = newLineFoldingLength[i];
						}
					//}
				}
				
			}
			finally {
				//UIDEThreadPool.UnregisterThread("SRCSUS_UpdateMultilineFormatting");
			}
			
			//editor.editorWindow.Repaint();
		}
		
		private List<StatementBlock> GetStatementBlocksRecursive(StatementBlock block) {
			List<StatementBlock> blocks = new List<StatementBlock>();
			foreach (Statement statement in block.statements) {
				if (statement.IsTypeOf<NamespaceDef>()) {
					NamespaceDef ns = (NamespaceDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(ns.statementBlock));
					blocks.Add(ns.statementBlock);
				}
				if (statement.IsTypeOf<TypeDef>()) {
					TypeDef t = (TypeDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(t.statementBlock));
					blocks.Add(t.statementBlock);
				}
				if (statement.IsTypeOf<StatementBlock>()) {
					StatementBlock sb = (StatementBlock)statement;
					blocks.AddRange(GetStatementBlocksRecursive(sb));
					blocks.Add(sb);
				}
				if (statement.IsTypeOf<MethodDef>()) {
					MethodDef m = (MethodDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(m.statementBlock));
					blocks.Add(m.statementBlock);
				}
				if (statement.IsTypeOf<PropertyDef>()) {
					PropertyDef p = (PropertyDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(p.statementBlock));
					blocks.Add(p.statementBlock);
				}
				if (statement.IsTypeOf<ForDef>()) {
					ForDef f = (ForDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(f.statementBlock));
					blocks.Add(f.statementBlock);
				}
				if (statement.IsTypeOf<ForEachDef>()) {
					ForEachDef f = (ForEachDef)statement;
					blocks.AddRange(GetStatementBlocksRecursive(f.statementBlock));
					blocks.Add(f.statementBlock);
				}
			}
			return blocks;
		}
		
	}
	
}