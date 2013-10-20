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

namespace UIDE.SyntaxRules.Generic {
	[System.Serializable]
	public class SyntaxRuleGeneric:SyntaxRule {
		
		private bool wantsMultiLineFormattingUpdate = false;
		
		public SyntaxRuleGeneric() {
			isDefault = true;
			shouldUseGenericAutoComplete = true;
			autoCompleteSubmitOnEnter = false;
		}
		
		public override void OnTextEditorUpdate() {
			if (wantsMultiLineFormattingUpdate) {
				if (UpdateMultilineFormatting()) {
					//wantsMultiLineFormattingUpdate = false;
				} 
			}
		}
		
		public override void Start() {
			
		}
		
		public override void OnFocus() {
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnSwitchToTab() {
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnRebuildLines(UIDEDoc doc) {
			
		}
		
		public override void OnPostBackspace() {
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnChangedCursorPosition(Vector2 pos) {
			
		}
		
		public override void OnPostEnterText(string text) {
			if (text == "\r" || text == "\n") {
				OnNewLine();
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
			if (lastElement != null && lastElement.rawText == "{") {
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
			return !(UIDE.SyntaxRules.CSharp.Keywords.keywordHash.Get(str) == null);
		}
		public override bool CheckIfStringIsModifier(string str) {
			return !(UIDE.SyntaxRules.CSharp.Keywords.modifierHash.Get(str) == null);
		}
		public override bool CheckIfStringIsPrimitiveType(string str) {
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
			return tokenDef;
		}
		
		public override CompletionItem[] GetGlobalCompletionItems() {
			
			/*
			string[] keywords = UIDE.SyntaxRules.CSharp.Keywords.keywords;
			string[] modifiers = UIDE.SyntaxRules.CSharp.Keywords.modifiers;
			string[] primitiveTypes = UIDE.SyntaxRules.CSharp.Keywords.primitiveTypes;
			
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
			*/
			
			return GetGenericCompletionItems();
		}
		
		public override bool CheckIfShouldLoad(UIDETextEditor textEditor) {
			return true;
		}
		
		public override void OnRebuildLineElements(UIDELine line) {
			base.OnRebuildLineElements(line);
			
			wantsMultiLineFormattingUpdate = true;
		}
		
		public bool UpdateMultilineFormatting() {
			if (useMultiThreading) {
				if (UIDEThreadPool.IsRegistered("SRG_UpdateMultilineFormatting")) {
					wantsMultiLineFormattingUpdate = true;
					return false;
				}
				wantsMultiLineFormattingUpdate = false;
				
				UIDEThreadPool.RegisterThread("SRG_UpdateMultilineFormatting",UpdateMultilineFormattingActual);
			}
			else {
				UpdateMultilineFormattingActual(null);
				wantsMultiLineFormattingUpdate = false;
			}
			return true;
		}
		private void UpdateMultilineFormattingActual(System.Object context) {
			try {
				UpdateMultilineFormattingGeneric();
			}
			finally {
				//UIDEThreadPool.UnregisterThread("SRG_UpdateMultilineFormatting");
			}
			//editor.editorWindow.Repaint();
		}
		
	}
	
}