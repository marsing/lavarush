using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System;
using System.IO;

using UnityEditor;

//TODO:

//Editor Features:
//File backup on save.

//UI:
//Implement simple field/property animation system.
//Add more stuff to ProjectView.
//	Recent files dropdown?
//	Plugin icon tray at the bottom?

public class UIDEEditorWindow:EditorWindow {
	//private Vector2 mousePos;
	//private Vector2 lastMousePos;
	static public UIDEEditorWindow current;
	static public EditorWindow lastFocusedWindow = null;
	static public HashSet<Type> toggleableWindowTypes = new HashSet<Type>(new Type[] {typeof(SceneView),System.Type.GetType("UnityEditor.GameView,UnityEditor")});
	
	public bool isPlaying = false;
	public bool isLoaded = false;
	
	[SerializeField]
	private UIDEEditor _editor;
	public UIDEEditor editor {
		get {
			return _editor;
		}
		set {
			
			_editor = value;
			_editor.rect = position;
			UIDEEditor.current = _editor;
			Repaint();
		}
	}
	
	[MenuItem ("Window/UnIDE %e")]
	static public void Init() {
		if (EditorWindow.focusedWindow != null && toggleableWindowTypes.Contains(EditorWindow.focusedWindow.GetType())) {
			lastFocusedWindow = EditorWindow.focusedWindow;
		}
		
		if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == typeof(UIDEEditorWindow) && lastFocusedWindow != null) {
			EditorWindow.FocusWindowIfItsOpen(lastFocusedWindow.GetType());
		}
		else {
			UIDEEditorWindow.Get();
		}
	}
	
	static public UIDEEditorWindow Get() {
		UIDEEditorWindow window = (UIDEEditorWindow)EditorWindow.GetWindow(typeof(UIDEEditorWindow),false,"UnIDE");
		return window;
	}
	
	public void EditorApplicationUpdate() {
		if (EditorWindow.focusedWindow != null && toggleableWindowTypes.Contains(EditorWindow.focusedWindow.GetType())) {
			lastFocusedWindow = EditorWindow.focusedWindow;
		}
		
		if (editor != null) {
			if (editor.shouldFocus) {
				editor.shouldFocus = false;
				Focus();
			}
		}
	}
	
	public void OnPlayStateChange() {
		isPlaying = Application.isPlaying;
	}
	
	public void OnProjectWindowItemGUI(string guid, Rect selectionRect) {
		if (editor != null) {
			editor.OnProjectWindowItemGUI(guid,selectionRect);
		}
	}
	
	public void OnEnable() {
		UIDEEditorWindow.current = this;
		
		EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
		EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
		
		EditorApplication.update -= EditorApplicationUpdate;
		EditorApplication.update += EditorApplicationUpdate;
		
		EditorApplication.playmodeStateChanged -= OnPlayStateChange;
		EditorApplication.playmodeStateChanged += OnPlayStateChange;
		
		if (isLoaded) return;
		this.autoRepaintOnSceneChange = false;
		this.wantsMouseMove = true;
		this.Start();
		isLoaded = true;
		
	}
	
	public void OnDisable() {
		EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
		EditorApplication.update -= EditorApplicationUpdate;
		EditorApplication.playmodeStateChanged -= OnPlayStateChange;
	}
	
	public void OnDestroy() {
		if (editor != null && !editor.isFocused) {
			//Hack to check that the window was intentionally closed and not the effect of a min/maximization.
			return;
		}
		
		//more hax
		if (editor != null && (!Application.isPlaying || !(Application.isPlaying && !isPlaying))) {
			editor.OnCloseWindow();
		}
	}
	
	public void Start() {
		Start(false);
	}
	public void Start(bool isReinit) {
		editor = new UIDEEditor();
		editor.Start(isReinit);
	}
	
	public void OnRequestSave() {
		if (editor != null) {
			editor.OnRequestSave();
		}
	}
	
	public void OnReloadScript(string fileName) {
		if (editor != null) {
			editor.OnReloadScript(fileName);
		}
	}
	
	void OnFocus() {
		if (editor != null) {
			editor.OnFocus();
		}
	}
	
	void OnLostFocus() {
		if (editor != null) {
			editor.OnLostFocus();
		}
	}
	
	bool IsFocused() {
		return EditorWindow.focusedWindow == this;
	}
	
	void Update() {
		if (EditorWindow.focusedWindow != null && toggleableWindowTypes.Contains(EditorWindow.focusedWindow.GetType())) {
			lastFocusedWindow = EditorWindow.focusedWindow;
		}
		this.wantsMouseMove = true;
		if (editor == null) {
			//Start(true);
			Close();
			return;
		}
		if (UIDEEditor.current != _editor) {
			UIDEEditor.current = _editor;
		}
		
		editor.isFocused = IsFocused();
		
		editor.rect = position;
		editor.Update();
		
		if (editor.wantsRepaint) {
			Repaint();
			editor.wantsRepaint = false;
		}
	}
	
	void OnGUI() {
		if (Event.current.type == EventType.MouseMove && IsFocused()) {
			Repaint();
		}
		
		if (editor == null) {
			Close();
			return;
		}
		
		if (UIDEEditor.current != _editor) {
			UIDEEditor.current = _editor;
		}
		
		editor.rect = position;
		BeginWindows();
		editor.OnGUI();
		EndWindows();
	}
}

