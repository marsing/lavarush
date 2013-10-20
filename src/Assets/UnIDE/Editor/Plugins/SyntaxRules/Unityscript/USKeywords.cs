using UnityEngine;
using System.Collections;

namespace UIDE.SyntaxRules.Unityscript {
	static public class Keywords:System.Object {
		static private UIDEHashTable _keywordHash;
		static public UIDEHashTable keywordHash {
			get {
				if (_keywordHash == null) {
					RebuildKeywordHash();
				}
				return _keywordHash;
			}
		}
		static private UIDEHashTable _modifierHash;
		static public UIDEHashTable modifierHash {
			get {
				if (_modifierHash == null) {
					RebuildModifierHash();
				}
				return _modifierHash;
			}
		}
		static private UIDEHashTable _primitiveTypeHash;
		static public UIDEHashTable primitiveTypeHash {
			get {
				if (_primitiveTypeHash == null) {
					RebuildPrimitiveTypeHash();
				}
				return _primitiveTypeHash;
			}
		}
		static public string[] modifiers = {
			"var",
			"function",
			"abstract",
			"const", 
			"default",
			"delegate",
			"explicit", 
			"extern", 
			"implicit", 
			"interface", 
			"internal", 
			"namespace", 
			"struct", 
			"class", 
			"extends",
			"new", 
			"operator", 
			"override", 
			"params", 
			"private", 
			"protected", 
			"public", 
			"readonly", 
			"ref", 
			"out", 
			"sealed", 
			"static",
			"import", 
			"virtual", 
			"volatile"
		};
		static public string[] primitiveTypes = {
			//"bool",
			"boolean", 
			"byte", 
			"char", 
			"double", 
			"enum", 
			"event", 
			"fixed", 
			"float", 
			"int", 
			"long", 
			"null", 
			"object", 
			"sbyte", 
			"short", 
			//"string", 
			"String", 
			"uint", 
			"ulong", 
			"ushort", 
			"void"
		};
		static public string[] keywords = {
			"as", 
			"cast", 
			"base", 
			"break", 
			"case", 
			"catch", 
			"checked", 
			"unchecked", 
			"continue", 
			"decimal", 
			"do", 
			"else", 
			"false", 
			"finally", 
			"for", 
			//"foreach", 
			"goto", 
			"if", 
			"in", 
			"is", 
			"lock", 
			"return", 
			"sizeof", 
			"stackalloc", 
			"switch", 
			"this", 
			"throw", 
			"true", 
			"try", 
			"typeof", 
			"unsafe", 
			"while",
			"set",
			"get",
			"yield"
		};
		
		static public void RebuildKeywordHash() {
			_keywordHash = new UIDEHashTable();
			for (int i = 0; i < keywords.Length; i++) {
				_keywordHash.Set(keywords[i],keywords[i]);
			}
		}
		static public void RebuildModifierHash() {
			_modifierHash = new UIDEHashTable();
			for (int i = 0; i < modifiers.Length; i++) {
				_modifierHash.Set(modifiers[i],modifiers[i]);
			}
		}
		static public void RebuildPrimitiveTypeHash() {
			_primitiveTypeHash = new UIDEHashTable();
			for (int i = 0; i < primitiveTypes.Length; i++) {
				_primitiveTypeHash.Set(primitiveTypes[i],primitiveTypes[i]);
			}
		}
	}
}
