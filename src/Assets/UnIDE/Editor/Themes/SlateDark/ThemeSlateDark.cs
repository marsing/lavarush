using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//using UIDE.Plugins;

namespace UIDE.Themes {
	[System.Serializable]
	public class ThemeSlateDark:Theme {
		static private Texture2D thumbnailTex;
		static public Texture2D GetThemeThumbnail() {
			if (thumbnailTex == null) {
				UnityEngine.Object obj = UIDEEditor.LoadAsset(themesFolder+"SlateDark/MenuPreview.psd");
				thumbnailTex = (Texture2D)obj;
			}
			return thumbnailTex;
		}
		
		static public string GetThemeFriendlyName() {
			return "Slate Dark";
		}
		
		public override void Start() {
			base.Start();
			themePath = "SlateDark/";
			LoadResourceTextures();
			LoadDefaultLanguageIcons();
			skin = (GUISkin)UIDEEditor.LoadAsset(fullThemePath+"Skin.guiskin");
			
			fontUpdateStyleNames.Add(new string[] {"TextEditorLabelNormal","Normal"});
			fontUpdateStyleNames.Add(new string[] {"TextEditorLabelBold","Bold"});
			fontUpdateStyleNames.Add(new string[] {"TextSelection","Bold"});
			fontUpdateStyleNames.Add(new string[] {"TextSelectionText","Bold"});
			fontUpdateStyleNames.Add(new string[] {"CursorHoverHighlight","Bold"});
			
			//UpdateFontSize(editor.textSettings.GetFontSize());
			UpdateFont(editor.textSettings.GetFont(),editor.textSettings.GetBoldFont());
		}
		
		public override void OnPreTextEditorGUI(Rect rect) {
			Texture bgPattern = GetStyle("TextEditorBackgroundPattern").normal.background;
			GUI.color = GetStyle("TextEditorBackgroundPattern").normal.textColor;
			Rect bgPatternUV = new Rect(0,0,rect.width/bgPattern.width,rect.height/bgPattern.height);
			GUI.DrawTextureWithTexCoords(rect,bgPattern,bgPatternUV);
			
			GUI.color = GetStyle("TextEditorBackgroundPattern").hover.textColor;
			Texture bgOverlay = GetStyle("TextEditorBackgroundPattern").hover.background;
			Rect bgOverlayUV = new Rect(0,0,rect.width/bgOverlay.width,(rect.height/bgOverlay.height));
			bgOverlayUV.y += 1.0f-bgOverlayUV.height;
			GUI.DrawTextureWithTexCoords(rect,bgOverlay,bgOverlayUV);
			GUI.color = Color.white;
		}
		public override void OnPostTextEditorGUI(Rect rect) {
			Texture bgOverlayTop = GetStyle("TextEditorBackgroundPattern").active.background;
			Rect bgOverlayTopUV = new Rect(0,0,rect.width/bgOverlayTop.width,(rect.height/bgOverlayTop.height));
			bgOverlayTopUV.y += 1.0f-bgOverlayTopUV.height;
			GUI.color = GetStyle("TextEditorBackgroundPattern").active.textColor;
			GUI.DrawTextureWithTexCoords(rect,bgOverlayTop,bgOverlayTopUV);
			GUI.color = Color.white;
		}
		
		public override void InitializeTokenDefs() {
			UIDETokenDefs.tokenDefsHash = new UIDEHashTable();
			
			UIDETokenDefs.AddNew("DefaultText",new Color(0.85f,0.85f,0.85f,1),new Color(1,1,1,0));
			
			UIDETokenDefs.AddNew("PreProcess",new Color(0.6f,0.6f,1.0f,1),new Color(1,1,1,0),1.0f,true);
			UIDETokenDefs.Get("PreProcess").isActualCode = false;
			
			UIDETokenDefs.AddNew("String",new Color(1.0f,0.85f,0.2f,1),new Color(1.0f,0.95f,0.4f,0.25f),1.25f,true);
			UIDETokenDefs.Get("String").useParsableString = true;
			UIDETokenDefs.Get("String").parsableString = "new System.String()";
			UIDETokenDefs.Get("String").isActualCode = false;
			
			UIDETokenDefs.AddNew("String,CharString",new Color(1.0f,0.85f,0.2f,1),new Color(1.0f,0.95f,0.4f,0.25f),1.25f,true);
			UIDETokenDefs.Get("String,CharString").useParsableString = true;
			UIDETokenDefs.Get("String,CharString").parsableString = "new System.Char()";
			UIDETokenDefs.Get("String,CharString").isActualCode = false;
			
			UIDETokenDefs.AddNew("Comment,SingleLine",new Color(0.5f,0.5f,0.5f,1),new Color(1,1,1,0.25f));
			UIDETokenDefs.AddNew("Comment,Block,Contained",new Color(0.5f,0.5f,0.5f,1),new Color(1,1,1,0.25f));
			UIDETokenDefs.AddNew("Comment,Block,Start",new Color(0.5f,0.5f,0.5f,1),new Color(1,1,1,0.25f));
			UIDETokenDefs.AddNew("Comment,Block,End",new Color(0.5f,0.5f,0.5f,1),new Color(1,1,1,0.25f));
			UIDETokenDefs.Get("Comment,SingleLine").isActualCode = false;
			UIDETokenDefs.Get("Comment,Block,Contained").isActualCode = false;
			UIDETokenDefs.Get("Comment,Block,Start").isActualCode = false;
			UIDETokenDefs.Get("Comment,Block,End").isActualCode = false;
			
			UIDETokenDefs.AddNew("Number,Double",new Color(1.0f,0.5f,0.35f,1),new Color(1.0f,0.75f,0.5f,0.25f),1.25f,true);
			UIDETokenDefs.Get("Number,Double").useParsableString = true;
			UIDETokenDefs.Get("Number,Double").parsableString = "new System.Double()";
			
			UIDETokenDefs.AddNew("Number,Float",new Color(1.0f,0.5f,0.35f,1),new Color(1.0f,0.75f,0.5f,0.25f),1.25f,true);
			UIDETokenDefs.Get("Number,Float").useParsableString = true;
			UIDETokenDefs.Get("Number,Float").parsableString = "new System.Single()";
			
			UIDETokenDefs.AddNew("Number,Int64",new Color(1.0f,0.5f,0.35f,1),new Color(1.0f,0.75f,0.5f,0.25f),1.25f,true);
			UIDETokenDefs.Get("Number,Int64").useParsableString = true;
			UIDETokenDefs.Get("Number,Int64").parsableString = "new System.Int64()";
			
			UIDETokenDefs.AddNew("Number,Int32",new Color(1.0f,0.5f,0.35f,1),new Color(1.0f,0.75f,0.5f,0.25f),1.25f,true);
			UIDETokenDefs.Get("Number,Int32").useParsableString = true;
			UIDETokenDefs.Get("Number,Int32").parsableString = "new System.Int32()";
			
			UIDETokenDefs.AddNew("Number",new Color(1.0f,0.5f,0.35f,1),new Color(1.0f,0.75f,0.5f,0.25f),1.25f,true);
			UIDETokenDefs.Get("Number").useParsableString = true;
			UIDETokenDefs.Get("Number").parsableString = "new System.Int32()";
			
			UIDETokenDefs.AddNew("Word",new Color(0.85f,0.85f,0.85f,1),new Color(1,1,1,0.0f),1.25f,true);
			UIDETokenDefs.AddNew("Word,Keyword",new Color(1.0f,1.0f,1.0f,1),new Color(1,1,1,0.0f),1.25f,true);
			UIDETokenDefs.AddNew("Word,Modifier",new Color(0.75f,0.5f,0.6f,1),new Color(1,1,1,0.0f),1.25f,true);
			UIDETokenDefs.AddNew("Word,PrimitiveType",new Color(0.65f,0.75f,0.5f,1),new Color(1,1,1,0.0f),1.25f,true);
			UIDETokenDefs.AddNew("Word,APIToken,Type",new Color(0.65f,0.75f,0.5f,1),new Color(1,1,1,0.0f),1.25f,true);
			//UIDETokenDefs.AddNew("Word,APIToken,Type",new Color(0.7f,0.9f,0.6f,1),new Color(1,1,1,0.0f),1.25f,true);
			
			UIDETokenDefs.AddNew("WhiteSpace",new Color(1,1,1,0),new Color(1,1,1,0));
			UIDETokenDefs.AddNew("WhiteSpace,Tab",new Color(1,1,1,0),new Color(1,1,1,0.0f));
			
			UIDETokenDefs.AddNew("LineEnd",new Color(1.0f,1.0f,1.0f,1),new Color(1,1,1,0),1.0f,true);
			UIDETokenDefs.AddNew("Dot",new Color(1.0f,1.0f,1.0f,1),new Color(1,1,1,0),1.0f,true);
			
			editor.textSettings.UpdateTokenDefBoldStates();
		}
	}
	
}
