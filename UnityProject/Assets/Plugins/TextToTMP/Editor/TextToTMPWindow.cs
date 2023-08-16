using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_2018_3_OR_NEWER
using PrefabStage = UnityEditor.SceneManagement.PrefabStage;
using PrefabStageUtility = UnityEditor.SceneManagement.PrefabStageUtility;
#endif

namespace TextToTMPNamespace
{
	public partial class TextToTMPWindow : EditorWindow, IHasCustomMenu
	{
		private enum UpgradeStage { InitializeAndCollectReferences, UpgradeScripts, PendingUpgradeScriptsCompletion, UpgradeComponents, UpdateReferences, Finished };

		#region Helper Classes
		[Serializable]
		private class FontUpgrade
		{
			public Font unityFont;
			public TMP_FontAsset tmpFont;
			public Material tmpFontMaterialDefault;
			public Material tmpFontMaterialShadow;
			public Material tmpFontMaterialOutline;
			public Material tmpFontMaterialShadowAndOutline;
		}

		[Serializable]
		private class SaveData
		{
			public List<Object> upgradeTargets;
			public List<FontUpgrade> fontUpgrades;
			public bool upgradeScenes;
			public bool alwaysUseOverflowForNonWrappingTexts;
			public bool saveDataValidityCheck;
		}
		#endregion

		private const string DONT_CLOSE_SCENES_WARNING = "Scene(s) inside the 'Assets & Scenes To Upgrade' list will automatically be opened in order to upgrade the GameObjects inside them. You MUST NOT close these scenes until all the steps are completed!";
		private const string BACKUP_PROJECT_WARNING = "Beyond this point, you can't undo your actions or go back. You are strongly recommended to backup your project before proceeding!";
		private const string DONT_CLOSE_WINDOW_WARNING = "After this step, you MUST NOT close this window until all the steps are completed! Otherwise, you won't be able to restore the references to the upgraded components.";
		private const string FIX_COMPILATION_ERRORS_WARNING = "If there are any compiler errors in the Console, then it means that some script(s) couldn't be upgraded completely. You should fix those errors manually before proceeding.";

		internal static readonly GUILayoutOption GL_WIDTH_15 = GUILayout.Width( 15f );
		private readonly GUILayoutOption GL_WIDTH_25 = GUILayout.Width( 25f );
		private readonly GUILayoutOption GL_HEIGHT_30 = GUILayout.Height( 30f );
		private readonly GUILayoutOption GL_EXPAND_WIDTH = GUILayout.ExpandWidth( true );
		private GUIStyle boldWordWrappedLabel;

		private UpgradeStage stage = UpgradeStage.InitializeAndCollectReferences;

		private List<Object> upgradeTargets = new List<Object>( 1 ) { null };
		private bool upgradeScenes = true;

		private List<FontUpgrade> fontUpgrades = new List<FontUpgrade>();

		private ObjectsToUpgradeList assetsToUpgrade = new ObjectsToUpgradeList();
		private ObjectsToUpgradeList scenesToUpgrade = new ObjectsToUpgradeList();

		private readonly StringBuilder stringBuilder = new StringBuilder( 8192 );
		private Vector2 scrollPos;

		[MenuItem( "Window/Upgrade Text to TMP" )]
		private static void Init()
		{
			TextToTMPWindow window = GetWindow<TextToTMPWindow>();
			window.titleContent = new GUIContent( "Text to TMP" );
			window.minSize = new Vector2( 300f, 200f );

			window.Show();
		}

		private void OnEnable()
		{
			if( fontUpgrades.Count == 0 )
				fontUpgrades.Add( new FontUpgrade() );

			// First font upgrade is the default one
			FontUpgrade defaultFontUpgrade = fontUpgrades[0];
			if( !defaultFontUpgrade.tmpFont )
			{
				defaultFontUpgrade.tmpFont = TMP_Settings.defaultFontAsset;
				if( !defaultFontUpgrade.tmpFont )
					defaultFontUpgrade.tmpFont = GetFirstAssetOfType<TMP_FontAsset>();

				if( defaultFontUpgrade.tmpFont )
				{
					defaultFontUpgrade.tmpFontMaterialDefault = defaultFontUpgrade.tmpFont.material;
					defaultFontUpgrade.tmpFontMaterialShadow = defaultFontUpgrade.tmpFontMaterialDefault;
					defaultFontUpgrade.tmpFontMaterialOutline = defaultFontUpgrade.tmpFontMaterialDefault;
					defaultFontUpgrade.tmpFontMaterialShadowAndOutline = defaultFontUpgrade.tmpFontMaterialDefault;
				}
			}
		}

		void IHasCustomMenu.AddItemsToMenu( GenericMenu menu )
		{
			if( stage == UpgradeStage.InitializeAndCollectReferences )
			{
				menu.AddItem( new GUIContent( "Save Settings" ), false, () =>
				{
					string savePath = EditorUtility.SaveFilePanel( "Save Settings To", "Library", "TextToTMP Settings", "json" );
					if( !string.IsNullOrEmpty( savePath ) )
					{
						File.WriteAllText( savePath, EditorJsonUtility.ToJson( new SaveData()
						{
							upgradeTargets = upgradeTargets,
							fontUpgrades = fontUpgrades,
							upgradeScenes = upgradeScenes,
							alwaysUseOverflowForNonWrappingTexts = alwaysUseOverflowForNonWrappingTexts,
							saveDataValidityCheck = true
						}, true ) );
					}
				} );

				menu.AddItem( new GUIContent( "Load Settings" ), false, () =>
				{
					string loadPath = EditorUtility.OpenFilePanel( "Load Settings From", "Library", "json" );
					if( !string.IsNullOrEmpty( loadPath ) )
					{
						SaveData saveData = new SaveData();
						EditorJsonUtility.FromJsonOverwrite( File.ReadAllText( loadPath ), saveData );
						if( !saveData.saveDataValidityCheck ) // If a random JSON file is loaded, it won't overwrite all the settings by mistake
							Debug.LogWarning( "Selected an invalid save data JSON file!" );
						else
						{
							upgradeTargets = saveData.upgradeTargets;
							fontUpgrades = saveData.fontUpgrades;
							upgradeScenes = saveData.upgradeScenes;
							alwaysUseOverflowForNonWrappingTexts = saveData.alwaysUseOverflowForNonWrappingTexts;

							Repaint();
						}
					}
				} );
			}
		}

		private void OnGUI()
		{
			if( EditorApplication.isPlaying )
			{
				EditorGUILayout.HelpBox( "Text To TMP can't work while in Play Mode.", MessageType.Error );
				return;
			}

			if( !AreScenesSaved() )
			{
				EditorGUILayout.HelpBox( "Text To TMP can't work while there are unsaved Scenes.", MessageType.Error );
				return;
			}

#if UNITY_2018_3_OR_NEWER
			UnityEditor.SceneManagement.PrefabStage openPrefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if( openPrefabStage != null && openPrefabStage.stageHandle.IsValid() && openPrefabStage.scene.isDirty )
			{
				EditorGUILayout.HelpBox( "Text To TMP can't work while there are unsaved changes in the Prefab stage.", MessageType.Error );
				return;
			}
#endif

			if( boldWordWrappedLabel == null )
				boldWordWrappedLabel = new GUIStyle( EditorStyles.boldLabel ) { wordWrap = true };

			scrollPos = GUILayout.BeginScrollView( scrollPos );

			Event ev = Event.current;

			if( stage == UpgradeStage.InitializeAndCollectReferences )
			{
				GUILayout.Box( "Assets & Scenes To Upgrade", GL_EXPAND_WIDTH );

				if( ( ev.type == EventType.DragPerform || ev.type == EventType.DragUpdated ) && GUILayoutUtility.GetLastRect().Contains( ev.mousePosition ) )
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					if( ev.type == EventType.DragPerform )
					{
						DragAndDrop.AcceptDrag();

						Object[] draggedObjects = DragAndDrop.objectReferences;
						if( draggedObjects != null )
						{
							for( int i = 0; i < draggedObjects.Length; i++ )
							{
								if( draggedObjects[i] && AssetDatabase.Contains( draggedObjects[i] ) )
									upgradeTargets.Add( draggedObjects[i] );
							}
						}
					}

					ev.Use();
				}

				for( int i = 0; i < upgradeTargets.Count; i++ )
				{
					GUILayout.BeginHorizontal();

					upgradeTargets[i] = EditorGUILayout.ObjectField( GUIContent.none, upgradeTargets[i], typeof( Object ), false );

					if( GUILayout.Button( "+", GL_WIDTH_25 ) )
						upgradeTargets.Insert( i + 1, null );

					if( GUILayout.Button( "-", GL_WIDTH_25 ) )
					{
						// Always keep at least 1 element
						if( upgradeTargets.Count > 1 )
							upgradeTargets.RemoveAt( i-- );
						else
							upgradeTargets[0] = null;
					}

					GUILayout.EndHorizontal();
				}

				EditorGUILayout.HelpBox( "You can add folder(s) to this list. Their contents will be included in the upgrade process.", MessageType.Info );

				EditorGUILayout.Space();

				upgradeScenes = EditorGUILayout.ToggleLeft( "Upgrade Objects In Scenes (If Any)", upgradeScenes );

				if( upgradeScenes )
					EditorGUILayout.HelpBox( DONT_CLOSE_SCENES_WARNING, MessageType.Warning );

				EditorGUILayout.Space();

				GUILayout.Box( "Font Upgrades", GL_EXPAND_WIDTH );

				if( ( ev.type == EventType.DragPerform || ev.type == EventType.DragUpdated ) && GUILayoutUtility.GetLastRect().Contains( ev.mousePosition ) )
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					if( ev.type == EventType.DragPerform )
					{
						DragAndDrop.AcceptDrag();

						Object[] draggedObjects = DragAndDrop.objectReferences;
						if( draggedObjects != null )
						{
							List<Font> draggedUnityFonts = new List<Font>();
							List<TMP_FontAsset> draggedTMPFonts = new List<TMP_FontAsset>();
							for( int i = 0; i < draggedObjects.Length; i++ )
							{
								if( draggedObjects[i] )
								{
									if( draggedObjects[i] is Font )
										draggedUnityFonts.Add( (Font) draggedObjects[i] );
									else if( draggedObjects[i] is TMP_FontAsset )
										draggedTMPFonts.Add( (TMP_FontAsset) draggedObjects[i] );
								}
							}

							for( int i = 0, count = Mathf.Max( draggedUnityFonts.Count, draggedTMPFonts.Count ); i < count; i++ )
							{
								FontUpgrade fontUpgrade;
								if( i < draggedUnityFonts.Count && i < draggedTMPFonts.Count )
									fontUpgrade = new FontUpgrade() { unityFont = draggedUnityFonts[i], tmpFont = draggedTMPFonts[i] };
								else if( i < draggedUnityFonts.Count )
									fontUpgrade = new FontUpgrade() { unityFont = draggedUnityFonts[i] };
								else
									fontUpgrade = new FontUpgrade() { tmpFont = draggedTMPFonts[i] };

								if( fontUpgrade.tmpFont )
								{
									fontUpgrade.tmpFontMaterialDefault = fontUpgrade.tmpFont.material;
									fontUpgrade.tmpFontMaterialShadow = fontUpgrade.tmpFontMaterialDefault;
									fontUpgrade.tmpFontMaterialOutline = fontUpgrade.tmpFontMaterialDefault;
									fontUpgrade.tmpFontMaterialShadowAndOutline = fontUpgrade.tmpFontMaterialDefault;
								}

								fontUpgrades.Add( fontUpgrade );
							}
						}
					}

					ev.Use();
				}

				EditorGUILayout.HelpBox( "There are 4 types of font materials:\n- Shadow Material: used for Text components with Shadow component\n- Outline Material: used for Text components with Outline component\n- Shadow & Outline Material: used for Text components with Shadow and Outline components\n- Default Material: used for the remaining Text components", MessageType.Info );

				for( int i = 0; i < fontUpgrades.Count; i++ )
				{
					GUILayout.BeginHorizontal();

					if( i == 0 )
						GUILayout.Label( "Default TMP Font", boldWordWrappedLabel );
					else
						GUILayout.Label( "TMP Font for: " + ( fontUpgrades[i].unityFont ? fontUpgrades[i].unityFont.name : "<Font not assigned>" ), boldWordWrappedLabel );

					if( GUILayout.Button( "+", GL_WIDTH_25 ) )
						fontUpgrades.Insert( i + 1, new FontUpgrade() );

					if( i > 0 && GUILayout.Button( "-", GL_WIDTH_25 ) )
						fontUpgrades.RemoveAt( i-- );

					GUILayout.EndHorizontal();

					if( i != 0 )
						fontUpgrades[i].unityFont = EditorGUILayout.ObjectField( "Unity Font", fontUpgrades[i].unityFont, typeof( Font ), false ) as Font;

					EditorGUI.BeginChangeCheck();
					fontUpgrades[i].tmpFont = EditorGUILayout.ObjectField( "TMP Font", fontUpgrades[i].tmpFont, typeof( TMP_FontAsset ), false ) as TMP_FontAsset;
					if( EditorGUI.EndChangeCheck() )
					{
						fontUpgrades[i].tmpFontMaterialDefault = fontUpgrades[i].tmpFont ? fontUpgrades[i].tmpFont.material : null;
						fontUpgrades[i].tmpFontMaterialShadow = fontUpgrades[i].tmpFontMaterialDefault;
						fontUpgrades[i].tmpFontMaterialOutline = fontUpgrades[i].tmpFontMaterialDefault;
						fontUpgrades[i].tmpFontMaterialShadowAndOutline = fontUpgrades[i].tmpFontMaterialDefault;
					}

					fontUpgrades[i].tmpFontMaterialDefault = EditorGUILayout.ObjectField( "TMP Font Default Material", fontUpgrades[i].tmpFontMaterialDefault, typeof( Material ), false ) as Material;
					fontUpgrades[i].tmpFontMaterialShadow = EditorGUILayout.ObjectField( "TMP Font Shadow Material", fontUpgrades[i].tmpFontMaterialShadow, typeof( Material ), false ) as Material;
					fontUpgrades[i].tmpFontMaterialOutline = EditorGUILayout.ObjectField( "TMP Font Outline Material", fontUpgrades[i].tmpFontMaterialOutline, typeof( Material ), false ) as Material;
					fontUpgrades[i].tmpFontMaterialShadowAndOutline = EditorGUILayout.ObjectField( "TMP Font Shadow & Outline Material", fontUpgrades[i].tmpFontMaterialShadowAndOutline, typeof( Material ), false ) as Material;
				}

				EditorGUILayout.Space();

				if( GUILayout.Button( "START", GL_HEIGHT_30 ) && ( !upgradeScenes || EditorUtility.DisplayDialog( "Warning", DONT_CLOSE_SCENES_WARNING.Replace( "&", "and" ), "Got it!", "Cancel" ) ) )
				{
					AssetDatabase.SaveAssets();

					EditorUtility.DisplayProgressBar( "Initializing...", "Please wait...", 0f );
					try
					{
						Initialize();
					}
					finally
					{
						EditorUtility.ClearProgressBar();
					}

					CollectReferences();
					SwitchStage( UpgradeStage.UpgradeScripts );
				}
			}
			else if( stage == UpgradeStage.UpgradeScripts )
			{
				GUILayout.Box( "Step 1/3: Upgrading Scripts", GL_EXPAND_WIDTH );

				GUILayout.Label( "Text, InputField, Dropdown and TextMesh terms used inside these scripts will be upgraded to their TextMesh Pro variants:", boldWordWrappedLabel );

				if( scriptsToUpgrade.Length > 0 )
					scriptsToUpgrade.DrawOnGUI();
				else
					GUILayout.Label( "<None>" );

				EditorGUILayout.Space();

#if UNITY_2017_3_OR_NEWER
				GUILayout.Label( "TextMesh Pro's Assembly Definition File (if exists) will be added to these Assembly Definition Files' 'Assembly Definition References' list:", boldWordWrappedLabel );

				if( assemblyDefinitionFilesToUpgrade.Length > 0 )
					assemblyDefinitionFilesToUpgrade.DrawOnGUI();
				else
					GUILayout.Label( "<None>" );

				EditorGUILayout.Space();
#endif

				EditorGUILayout.HelpBox( BACKUP_PROJECT_WARNING, MessageType.Warning );
				EditorGUILayout.HelpBox( DONT_CLOSE_WINDOW_WARNING, MessageType.Warning );

				EditorGUILayout.Space();

#if UNITY_2017_3_OR_NEWER
				GUI.enabled = scriptsToUpgrade.EnabledCount > 0 | assemblyDefinitionFilesToUpgrade.EnabledCount > 0;
#else
				GUI.enabled = scriptsToUpgrade.EnabledCount > 0;
#endif
				if( GUILayout.Button( "UPGRADE SCRIPTS", GL_HEIGHT_30 ) && EditorUtility.DisplayDialog( "Warning", BACKUP_PROJECT_WARNING, "Got it!", "Cancel" ) && EditorUtility.DisplayDialog( "Warning", DONT_CLOSE_WINDOW_WARNING, "Got it!", "Cancel" ) )
				{
					AssetDatabase.SaveAssets();
					UpgradeScripts();
					SwitchStage( UpgradeStage.PendingUpgradeScriptsCompletion );
				}
				GUI.enabled = true;

				EditorGUILayout.Space();

				if( GUILayout.Button( "DON'T UPGRADE SCRIPTS", GL_HEIGHT_30 ) && EditorUtility.DisplayDialog( "Warning", BACKUP_PROJECT_WARNING, "Got it!", "Cancel" ) && EditorUtility.DisplayDialog( "Warning", DONT_CLOSE_WINDOW_WARNING, "Got it!", "Cancel" ) )
					SwitchStage( UpgradeStage.UpgradeComponents );

				EditorGUILayout.Space();

				if( GUILayout.Button( "BACK", GL_HEIGHT_30 ) )
					SwitchStage( UpgradeStage.InitializeAndCollectReferences );
			}
			else if( stage == UpgradeStage.PendingUpgradeScriptsCompletion )
			{
				GUILayout.Box( "Step 1/3: Upgrading Scripts", GL_EXPAND_WIDTH );

				if( EditorApplication.isCompiling )
					GUILayout.Label( "Waiting for Unity to finish compiling the scripts...", boldWordWrappedLabel );
				else
					GUILayout.Label( FIX_COMPILATION_ERRORS_WARNING, boldWordWrappedLabel );

				EditorGUILayout.Space();

				GUI.enabled = !EditorApplication.isCompiling;
				if( GUILayout.Button( "NEXT STEP", GL_HEIGHT_30 ) && EditorUtility.DisplayDialog( "Warning", FIX_COMPILATION_ERRORS_WARNING, "Got it!", "Cancel" ) )
					SwitchStage( UpgradeStage.UpgradeComponents );

				GUI.enabled = true;
			}
			else if( stage == UpgradeStage.UpgradeComponents )
			{
				GUILayout.Box( "Step 2/3: Upgrading Components", GL_EXPAND_WIDTH );

				GUILayout.Label( "Text, InputField, Dropdown and TextMesh component(s) on the following assets will be upgraded to their TextMesh Pro variants:", boldWordWrappedLabel );

				if( assetsToUpgrade.Length > 0 )
					assetsToUpgrade.DrawOnGUI();
				else
					GUILayout.Label( "<None>" );

				EditorGUILayout.Space();

				GUILayout.Label( "The same component(s) inside these Scenes will also be upgraded:", boldWordWrappedLabel );

				if( scenesToUpgrade.Length > 0 )
					scenesToUpgrade.DrawOnGUI();
				else
					GUILayout.Label( "<None>" );

				EditorGUILayout.Space();

				if( assetsToUpgrade.EnabledCount > 0 || scenesToUpgrade.EnabledCount > 0 )
				{
					GUILayout.Label( "When a Text's 'Horizontal Overflow' is set to 'Overflow' and 'Vertical Overflow' is set to 'Truncate', its behaviour will change when it is upgraded to TextMesh Pro:", boldWordWrappedLabel );

					alwaysUseOverflowForNonWrappingTexts = !WordWrappingToggleLeft( "Set TextMesh Pro's 'Overflow' value to 'Truncate': text will truncate both horizontally and vertically, i.e. it won't overflow horizontally", !alwaysUseOverflowForNonWrappingTexts );
					alwaysUseOverflowForNonWrappingTexts = WordWrappingToggleLeft( "Set TextMesh Pro's 'Overflow' value to 'Overflow': text will overflow both horizontally and vertically, i.e. it won't truncate vertically", alwaysUseOverflowForNonWrappingTexts );

					EditorGUILayout.Space();
				}

				GUI.enabled = assetsToUpgrade.EnabledCount > 0 || scenesToUpgrade.EnabledCount > 0;
				if( GUILayout.Button( "UPGRADE COMPONENTS", GL_HEIGHT_30 ) )
				{
					AssetDatabase.SaveAssets();
					UpgradeComponents();
					SwitchStage( UpgradeStage.UpdateReferences );
				}
				GUI.enabled = true;

				EditorGUILayout.Space();

				if( GUILayout.Button( "DON'T UPGRADE COMPONENTS", GL_HEIGHT_30 ) )
					SwitchStage( UpgradeStage.UpdateReferences );
			}
			else if( stage == UpgradeStage.UpdateReferences )
			{
				GUILayout.Box( "Step 3/3: Reconnecting References", GL_EXPAND_WIDTH );

				GUILayout.Label( "For objects whose variables were referencing the upgraded Text, InputField, Dropdown and TextMesh components, these variables will now refer to the components' TextMesh Pro variants.", boldWordWrappedLabel );

				EditorGUILayout.Space();

				if( GUILayout.Button( "RECONNECT REFERENCES", GL_HEIGHT_30 ) )
				{
					AssetDatabase.SaveAssets();
					UpdateReferences();
					SwitchStage( UpgradeStage.Finished );
				}

				EditorGUILayout.Space();

				if( GUILayout.Button( "DON'T RECONNECT REFERENCES", GL_HEIGHT_30 ) )
					SwitchStage( UpgradeStage.Finished );
			}
			else if( stage == UpgradeStage.Finished )
			{
				GUILayout.Box( "Upgrade Completed", GL_EXPAND_WIDTH );
				GUILayout.Label( "You can now safely close this window and close the scenes that were opened during the upgrade process (if any).", boldWordWrappedLabel );

				EditorGUILayout.Space();

				if( GUILayout.Button( "START AGAIN", GL_HEIGHT_30 ) )
					SwitchStage( UpgradeStage.InitializeAndCollectReferences );
			}

			EditorGUILayout.Space();
			GUILayout.EndScrollView();
		}

		private void SwitchStage( UpgradeStage stage )
		{
			this.stage = stage;
			scrollPos = Vector2.zero;

			GUI.enabled = true;
			GUIUtility.ExitGUI();
		}

		private void Initialize()
		{
			assetsToUpgrade.Clear();
			scenesToUpgrade.Clear();
			scriptsToUpgrade.Clear();
#if UNITY_2017_3_OR_NEWER
			assemblyDefinitionFilesToUpgrade.Clear();
#endif

			HashSet<string> upgradeTargetsSet = new HashSet<string>();
			for( int i = 0; i < upgradeTargets.Count; i++ )
			{
				if( !upgradeTargets[i] )
					continue;

				string assetPath = AssetDatabase.GetAssetPath( upgradeTargets[i] );
				if( string.IsNullOrEmpty( assetPath ) )
					continue;

				if( !AssetDatabase.IsValidFolder( assetPath ) )
					upgradeTargetsSet.Add( assetPath );
				else
				{
					// This is a folder, collect references from assets inside it
					string[] folderContents = AssetDatabase.FindAssets( "", new string[] { assetPath } );
					if( folderContents == null )
						continue;

					for( int j = 0; j < folderContents.Length; j++ )
					{
						assetPath = AssetDatabase.GUIDToAssetPath( folderContents[j] );
						if( !string.IsNullOrEmpty( assetPath ) && !AssetDatabase.IsValidFolder( assetPath ) )
							upgradeTargetsSet.Add( assetPath );
					}
				}
			}

			foreach( string assetPath in upgradeTargetsSet )
			{
				if( assetPath.EndsWith( ".unity", StringComparison.OrdinalIgnoreCase ) )
				{
					if( upgradeScenes && typeof( SceneAsset ).IsAssignableFrom( AssetDatabase.GetMainAssetTypeAtPath( assetPath ) ) )
					{
						scenesToUpgrade.Add( assetPath );

						// Load unloaded scenes
						Scene scene = SceneManager.GetSceneByPath( assetPath );
						if( !scene.isLoaded )
							EditorSceneManager.OpenScene( assetPath, OpenSceneMode.Additive );
					}
				}
				if( assetPath.EndsWith( ".cs", StringComparison.OrdinalIgnoreCase ) )
					AddScriptToUpgrade( assetPath );
#if UNITY_2017_3_OR_NEWER
				else if( assetPath.EndsWith( ".asmdef", StringComparison.OrdinalIgnoreCase ) )
					assemblyDefinitionFilesToUpgrade.Add( assetPath );
#endif
				else if( AssetDatabase.LoadAssetAtPath<GameObject>( assetPath ) || AssetDatabase.LoadAssetAtPath<ScriptableObject>( assetPath ) )
					assetsToUpgrade.Add( assetPath );
			}
		}

		private TMP_FontAsset GetCorrespondingTMPFontAsset( Font font )
		{
			if( !font )
				return null;

			for( int i = 1; i < fontUpgrades.Count; i++ )
			{
				if( fontUpgrades[i].unityFont == font )
					return fontUpgrades[i].tmpFont;
			}

			// 0 is the default TMP font, check it last
			return fontUpgrades[0].tmpFont;
		}

		private TMP_FontAsset GetCorrespondingTMPFontAsset( Font font, Component source, out Material fontMaterial )
		{
			FontUpgrade fontUpgrade = fontUpgrades[0];
			for( int i = 1; i < fontUpgrades.Count; i++ )
			{
				if( fontUpgrades[i].unityFont == font )
				{
					fontUpgrade = fontUpgrades[i];
					break;
				}
			}

			TMP_FontAsset fontAsset = fontUpgrade.tmpFont;
			if( !source )
				fontMaterial = fontUpgrade.tmpFontMaterialDefault;
			else
			{
				BaseMeshEffect[] modifierComponents = source.GetComponents<BaseMeshEffect>();

				bool shadowEnabled = false;
				bool outlineEnabled = false;
				for( int i = 0; i < modifierComponents.Length; i++ )
				{
					if( modifierComponents[i] && modifierComponents[i].enabled )
					{
						// We aren't using "modifierComponents[i] is Shadow" because Outline component derives from Shadow
						if( modifierComponents[i].GetType() == typeof( Shadow ) )
							shadowEnabled = true;
						else if( modifierComponents[i].GetType() == typeof( Outline ) )
							outlineEnabled = true;
					}
				}

				// For some unknown reason, calling DestroyImmediate while iterating the array causes strange issues like a Shadow component suddenly becoming
				// an Outline component. Thus, we are destroying the components only after assigning the shadowEnabled and outlineEnabled's values correctly
				for( int i = 0; i < modifierComponents.Length; i++ )
				{
					if( modifierComponents[i] && ( modifierComponents[i].GetType() == typeof( Shadow ) || modifierComponents[i].GetType() == typeof( Outline ) ) )
					{
						stringBuilder.Append( "Removing " ).Append( modifierComponents[i].GetType().Name ).Append( " component from " ).AppendLine( GetPathOfObject( source.transform ) );
						DestroyImmediate( modifierComponents[i], true );
					}
				}

				if( shadowEnabled )
				{
					if( outlineEnabled )
						fontMaterial = fontUpgrade.tmpFontMaterialShadowAndOutline;
					else
						fontMaterial = fontUpgrade.tmpFontMaterialShadow;
				}
				else if( outlineEnabled )
					fontMaterial = fontUpgrade.tmpFontMaterialOutline;
				else
					fontMaterial = fontUpgrade.tmpFontMaterialDefault;
			}

			if( !fontMaterial )
				fontMaterial = fontAsset.material;

			return fontAsset;
		}

		internal static bool WordWrappingToggleLeft( string label, bool value )
		{
			GUILayout.BeginHorizontal();
			bool result = EditorGUILayout.ToggleLeft( GUIContent.none, value, GL_WIDTH_15 );
			if( GUILayout.Button( label, EditorStyles.wordWrappedLabel ) )
			{
				GUI.FocusControl( null );
				result = !value;
			}
			GUILayout.EndHorizontal();

			return result;
		}
	}
}