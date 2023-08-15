using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TextToTMPNamespace
{
	public partial class TextToTMPWindow
	{
		#region Constants
		private const string TMP_INPUT_FIELD_TEXT_AREA_NAME = "Text Area";
		#endregion

		private bool alwaysUseOverflowForNonWrappingTexts = false;

		private readonly HashSet<string> upgradedPrefabs = new HashSet<string>();
		private readonly List<PrefabInstancesRemovedComponent> upgradedComponentsToRemoveInPrefabInstances = new List<PrefabInstancesRemovedComponent>();

		private FieldInfo unityEventPersistentCallsField;
#if UNITY_2018_3_OR_NEWER
		private MethodInfo prefabCyclicReferenceCheckerMethod;
#endif
		private MethodInfo rectTransformAnchorsSetterMethod;

		private void UpgradeComponents()
		{
			upgradedPrefabs.Clear();

			List<GameObject> prefabsToUpgrade = new List<GameObject>( assetsToUpgrade.Length );
			HashSet<string> prefabsToUpgradePaths = new HashSet<string>();

			stringBuilder.Length = 0;

			int progressCurrent = 0;
			int progressTotal = assetsToUpgrade.EnabledCount + scenesToUpgrade.EnabledCount + modifiedTextPrefabInstances.Count + modifiedTextMeshPrefabInstances.Count + modifiedInputFieldPrefabInstances.Count + modifiedDropdownPrefabInstances.Count;
			try
			{
				foreach( string asset in assetsToUpgrade )
				{
					EditorUtility.DisplayProgressBar( "Upgrading components in assets...", asset, (float) progressCurrent / progressTotal );
					progressCurrent++;

					if( asset.EndsWith( ".prefab", StringComparison.OrdinalIgnoreCase ) && prefabsToUpgradePaths.Add( asset ) )
					{
						GameObject prefab = AssetDatabase.LoadMainAssetAtPath( asset ) as GameObject;
						if( ShouldUpgradeComponentsInPrefab( prefab ) )
						{
							prefabsToUpgrade.Add( prefab );
							progressTotal++;
						}
					}
				}

#if UNITY_2018_3_OR_NEWER
				// Upgrade base prefabs before their variant prefabs so that changes to the base prefabs are reflected to their
				// variant prefabs before we start upgrading those variant prefabs
				if( prefabCyclicReferenceCheckerMethod == null )
					prefabCyclicReferenceCheckerMethod = typeof( PrefabUtility ).GetMethod( "CheckIfAddingPrefabWouldResultInCyclicNesting", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static );

				prefabsToUpgrade.Sort( ( prefab1, prefab2 ) => (bool) prefabCyclicReferenceCheckerMethod.Invoke( null, new object[] { prefab1, prefab2 } ) ? -1 : 1 );
#endif

				foreach( GameObject prefab in prefabsToUpgrade )
				{
					EditorUtility.DisplayProgressBar( "Upgrading components in prefabs...", AssetDatabase.GetAssetPath( prefab ), (float) progressCurrent / progressTotal );
					progressCurrent++;

					UpgradeComponentsInPrefab( prefab );
				}

				foreach( string scene in scenesToUpgrade )
				{
					EditorUtility.DisplayProgressBar( "Upgrading components in scenes...", scene, (float) progressCurrent / progressTotal );
					progressCurrent++;

					UpgradeComponentsInScene( SceneManager.GetSceneByPath( scene ) );
				}

				foreach( TextProperties modifiedPrefabInstance in modifiedTextPrefabInstances )
					ApplyPrefabInstanceModifications( modifiedPrefabInstance, ( tmp ) => PasteTextProperties( tmp, modifiedPrefabInstance ), ref progressCurrent, progressTotal );
				foreach( TextMeshProperties modifiedPrefabInstance in modifiedTextMeshPrefabInstances )
					ApplyPrefabInstanceModifications( modifiedPrefabInstance, ( tmp ) => PasteTextMeshProperties( tmp, modifiedPrefabInstance ), ref progressCurrent, progressTotal );
				foreach( InputFieldProperties modifiedPrefabInstance in modifiedInputFieldPrefabInstances )
					ApplyPrefabInstanceModifications( modifiedPrefabInstance, ( tmp ) => PasteInputFieldProperties( tmp, modifiedPrefabInstance ), ref progressCurrent, progressTotal );
				foreach( DropdownProperties modifiedPrefabInstance in modifiedDropdownPrefabInstances )
					ApplyPrefabInstanceModifications( modifiedPrefabInstance, ( tmp ) => PasteDropdownProperties( tmp, modifiedPrefabInstance ), ref progressCurrent, progressTotal );
			}
			finally
			{
				if( stringBuilder.Length > 0 )
				{
					stringBuilder.Insert( 0, "<b>Upgrade Components Logs:</b>\n" );
					Debug.Log( stringBuilder.ToString() );
				}

				EditorUtility.ClearProgressBar();

				AssetDatabase.SaveAssets();
				EditorSceneManager.SaveOpenScenes();
			}
		}

		private void UpgradeComponentsInScene( Scene scene )
		{
			if( !scene.IsValid() )
			{
				Debug.LogError( "Scene " + scene.name + " is not valid, can't uprade components inside it!" );
				return;
			}

			if( !scene.isLoaded )
			{
				Debug.LogError( "Scene " + scene.name + " is not loaded, can't uprade components inside it!" );
				return;
			}

			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			for( int i = 0; i < rootGameObjects.Length; i++ )
				UpgradeGameObjectRecursively( rootGameObjects[i].transform, null );

			EditorSceneManager.MarkSceneDirty( scene );
		}

		private bool ShouldUpgradeComponentsInPrefab( GameObject prefab )
		{
			if( !prefab )
				return false;

			if( ( prefab.hideFlags & HideFlags.NotEditable ) == HideFlags.NotEditable )
				return false;

#if UNITY_2018_3_OR_NEWER
			PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType( prefab );
			if( prefabAssetType != PrefabAssetType.Regular && prefabAssetType != PrefabAssetType.Variant )
				return false;
#else
			PrefabType prefabAssetType = PrefabUtility.GetPrefabType( prefab );
			if( prefabAssetType != PrefabType.Prefab )
				return false;
#endif

			// Check if prefab has any upgradeable components
			if( prefab.GetComponentInChildren<TextMesh>( true ) )
				return true;

			foreach( UIBehaviour graphicComponent in prefab.GetComponentsInChildren<UIBehaviour>( true ) )
			{
				if( graphicComponent && ( graphicComponent is Text || graphicComponent is InputField || graphicComponent is Dropdown ) )
					return true;
			}

			return false;
		}

		private void UpgradeComponentsInPrefab( GameObject prefab )
		{
			// Instantiate the prefab, we need instances to change children's parents (needed in InputField's upgrade)
#if UNITY_2018_3_OR_NEWER
			string prefabPath = AssetDatabase.GetAssetPath( prefab );
			GameObject prefabInstanceRoot = PrefabUtility.LoadPrefabContents( prefabPath );
#else
			GameObject prefabInstanceRoot = (GameObject) PrefabUtility.InstantiatePrefab( prefab );
			PrefabUtility.DisconnectPrefabInstance( prefabInstanceRoot );
#endif

			upgradedComponentsToRemoveInPrefabInstances.Clear();

			try
			{
				UpgradeGameObjectRecursively( prefabInstanceRoot.transform, prefab.transform );


#if UNITY_2018_3_OR_NEWER
				prefab = PrefabUtility.SaveAsPrefabAsset( prefabInstanceRoot, prefabPath );
#else
				prefab = PrefabUtility.ReplacePrefab( prefabInstanceRoot, prefab, ReplacePrefabOptions.ConnectToPrefab );
#endif
			}
			finally
			{
#if UNITY_2018_3_OR_NEWER
				PrefabUtility.UnloadPrefabContents( prefabInstanceRoot );
#else
				DestroyImmediate( prefabInstanceRoot );
#endif
			}

			// Remove upgraded Text, InputField, Dropdown and TextMesh components from the prefab instances that
			// originally had these components' legacy versions removed as prefab override, as well
			foreach( PrefabInstancesRemovedComponent removedComponentHolder in upgradedComponentsToRemoveInPrefabInstances )
			{
				if( !removedComponentHolder.componentOwner )
					continue;

				foreach( GameObject prefabInstance in removedComponentHolder.removedPrefabInstances )
				{
					if( !prefabInstance )
						continue;

					foreach( Component component in prefabInstance.GetComponents( removedComponentHolder.UpgradedComponentType ) )
					{
						if( !component )
							continue;

#if UNITY_2018_3_OR_NEWER
						Component prefabComponent = PrefabUtility.GetCorrespondingObjectFromSource( component );
#else
						Component prefabComponent = (Component) PrefabUtility.GetPrefabParent( component );
#endif
						if( prefabComponent && prefabComponent.gameObject == removedComponentHolder.componentOwner )
						{
							stringBuilder.Append( "Removing " ).Append( component.GetType().Name ).Append( " from " ).Append( GetPathOfObject( component.transform ) ).AppendLine( " since its legacy version was also removed as prefab override" );

							DestroyImmediate( component, true );
							EditorUtility.SetDirty( prefabInstance );

							break;
						}
					}
				}
			}

			EditorUtility.SetDirty( prefab );
			AssetDatabase.SaveAssets();
		}

		private void UpgradeGameObjectRecursively( Transform transform, Transform prefabTransform )
		{
			try
			{
				UpgradeDropdown( transform.GetComponent<Dropdown>(), prefabTransform ? prefabTransform.GetComponent<Dropdown>() : null );
				TMP_InputField inputField = UpgradeInputField( transform.GetComponent<InputField>(), prefabTransform ? prefabTransform.GetComponent<InputField>() : null, false );
				UpgradeText( transform.GetComponent<Text>(), prefabTransform ? prefabTransform.GetComponent<Text>() : null );
				UpgradeTextMesh( transform.GetComponent<TextMesh>(), prefabTransform ? prefabTransform.GetComponent<TextMesh>() : null );

				for( int i = 0; i < transform.childCount; i++ )
					UpgradeGameObjectRecursively( transform.GetChild( i ), prefabTransform ? prefabTransform.GetChild( i ) : null );

				// TMP_InputField objects have an extra Viewport (Text Area) child object, create it if necessary. We're creating that Viewport after traversing
				// the InputField's children because this operation can change some of those children's parents and while traversing hierarchies, we want
				// transform and prefabTransform to be synchronized
				if( inputField )
					CreateInputFieldViewport( inputField );
			}
			catch( Exception e )
			{
				if( !e.Data.Contains( "LoggedHierarchy" ) )
				{
					e.Data.Add( "LoggedHierarchy", true );
					Debug.LogError( "Error while upgrading components of: " + GetPathOfObject( transform ), prefabTransform ? prefabTransform.root.gameObject : transform.gameObject );
				}

				throw;
			}
		}

		private TextMeshProUGUI UpgradeText( Text text, Text prefabText )
		{
			if( !text )
				return null;

			OnComponentIsBeingUpgraded( text, prefabText );

			GameObject go = text.gameObject;
			stringBuilder.Append( "Upgrading Text: " ).AppendLine( GetPathOfObject( go.transform ) );

			// Copy fields
			Vector2 sizeDelta = text.rectTransform.sizeDelta;
			TextProperties properties = CopyTextProperties( text );

			// Replace Text with TextMeshProUGUI
			DestroyImmediate( text, true );
			TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();

			// Paste fields
			PasteTextProperties( tmp, properties );
			tmp.rectTransform.sizeDelta = sizeDelta;

			return tmp;
		}

		private TextProperties CopyTextProperties( Text text )
		{
			Material fontMaterial;
			TMP_FontAsset font = GetCorrespondingTMPFontAsset( text.font, text, out fontMaterial );

			return new TextProperties()
			{
				gameObject = text.gameObject,
				alignment = GetTMPAlignment( text.alignment, text.alignByGeometry ),
				bestFit = text.resizeTextForBestFit,
				bestFitMaxSize = text.resizeTextMaxSize,
				bestFitMinSize = text.resizeTextMinSize,
				color = text.color,
				enabled = text.enabled,
				fontMaterial = fontMaterial,
				font = font,
				fontSize = text.fontSize,
				fontStyle = GetTMPFontStyle( text.fontStyle ),
				horizontalWrapMode = text.horizontalOverflow == HorizontalWrapMode.Wrap,
				lineSpacing = ( text.lineSpacing - 1 ) * 100f,
				raycastTarget = text.raycastTarget,
				supportRichText = text.supportRichText,
				text = text.text,
				verticalOverflow = GetTMPVerticalOverflow( text.verticalOverflow, text.horizontalOverflow )
			};
		}

		private void PasteTextProperties( TextMeshProUGUI tmp, TextProperties properties )
		{
			tmp.alignment = properties.alignment;
			tmp.enableAutoSizing = properties.bestFit;
			tmp.fontSizeMax = properties.bestFitMaxSize;
			tmp.fontSizeMin = properties.bestFitMinSize;
			tmp.color = properties.color;
			tmp.enabled = properties.enabled;
			tmp.font = properties.font;
			tmp.fontMaterial = properties.fontMaterial;
			tmp.fontSize = properties.fontSize;
			tmp.fontStyle = properties.fontStyle;
			tmp.enableWordWrapping = properties.horizontalWrapMode;
			tmp.lineSpacing = properties.lineSpacing;
			tmp.raycastTarget = properties.raycastTarget;
			tmp.richText = properties.supportRichText;
			tmp.text = properties.text;
			tmp.overflowMode = properties.verticalOverflow;
		}

		private TextMeshPro UpgradeTextMesh( TextMesh text, TextMesh prefabText )
		{
			if( !text )
				return null;

			OnComponentIsBeingUpgraded( text, prefabText );

			GameObject go = text.gameObject;
			stringBuilder.Append( "Upgrading TextMesh: " ).AppendLine( GetPathOfObject( go.transform ) );

			// Copy fields
			TextMeshProperties properties = CopyTextMeshProperties( text );

			// Replace TextMesh with TextMeshPro
			DestroyImmediate( text, true );
			TextMeshPro tmp = go.AddComponent<TextMeshPro>();

			// Paste fields
			PasteTextMeshProperties( tmp, properties );

			tmp.enableWordWrapping = false;
			tmp.overflowMode = TextOverflowModes.Overflow;
			tmp.rectTransform.sizeDelta = Vector2.zero;

			return tmp;
		}

		private TextMeshProperties CopyTextMeshProperties( TextMesh text )
		{
			Material fontMaterial;
			TMP_FontAsset font = GetCorrespondingTMPFontAsset( text.font, text, out fontMaterial );

			return new TextMeshProperties()
			{
				gameObject = text.gameObject,
				alignment = GetTMPAlignment( text.anchor, false ),
				characterSize = text.characterSize,
				color = text.color,
				fontMaterial = fontMaterial,
				font = font,
				fontSize = text.fontSize > 0 ? text.fontSize : 13,
				fontStyle = GetTMPFontStyle( text.fontStyle ),
				lineSpacing = ( text.lineSpacing - 1 ) * 100f,
				offsetZ = text.offsetZ,
				richText = text.richText,
				text = text.text
			};
		}

		private void PasteTextMeshProperties( TextMeshPro tmp, TextMeshProperties properties )
		{
			tmp.alignment = properties.alignment;
			tmp.color = properties.color;
			tmp.font = properties.font;
			tmp.fontMaterial = properties.fontMaterial;
			tmp.fontSize = properties.fontSize;
			tmp.fontStyle = properties.fontStyle;
			tmp.lineSpacing = properties.lineSpacing;
			tmp.richText = properties.richText;
			tmp.text = properties.text;
			tmp.rectTransform.localScale *= properties.characterSize;
			tmp.rectTransform.Translate( new Vector3( 0f, 0f, properties.offsetZ ) );
		}

		private TMP_InputField UpgradeInputField( InputField inputField, InputField prefabInputField, bool createViewportImmediately = true )
		{
			if( !inputField )
				return null;

			OnComponentIsBeingUpgraded( inputField, prefabInputField );

			GameObject go = inputField.gameObject;
			stringBuilder.Append( "Upgrading InputField: " ).AppendLine( GetPathOfObject( go.transform ) );

			// Copy fields
			Vector2 sizeDelta = ( (RectTransform) inputField.transform ).sizeDelta;
			InputFieldProperties properties = CopyInputFieldProperties( inputField );

			// Upgrade child objects
			TextMeshProUGUI textComponent = UpgradeText( inputField.textComponent, prefabInputField ? prefabInputField.textComponent : null );
			Graphic placeholderComponent = ( inputField.placeholder as Text ) ? UpgradeText( (Text) inputField.placeholder, prefabInputField ? prefabInputField.placeholder as Text : null ) : inputField.placeholder;

			// Apply the changes that TMP_DefaultControls.cs applies by default to new TMP_InputFields
			if( textComponent ) textComponent.extraPadding = true;
			if( textComponent ) textComponent.overflowMode = TextOverflowModes.Overflow;
			if( placeholderComponent as TextMeshProUGUI ) ( (TextMeshProUGUI) placeholderComponent ).extraPadding = true;
			if( placeholderComponent && !placeholderComponent.GetComponent<LayoutElement>() ) placeholderComponent.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

			// Replace InputField with TMP_InputField
			DestroyImmediate( inputField, true );
			TMP_InputField tmp = go.AddComponent<TMP_InputField>();

			// Paste fields
			PasteInputFieldProperties( tmp, properties );
			( (RectTransform) tmp.transform ).sizeDelta = sizeDelta;

			// TMP_InputField objects have an extra Viewport (Text Area) child object, create it if necessary
			if( createViewportImmediately )
				CreateInputFieldViewport( tmp );

			return tmp;
		}

		private InputFieldProperties CopyInputFieldProperties( InputField inputField )
		{
			Color? caretColor = null;
			try { caretColor = inputField.caretColor; } catch { }

			return new InputFieldProperties()
			{
				gameObject = inputField.gameObject,
				selectableProperties = new SelectableObjectProperties( inputField ),

				textComponentGameObject = inputField.textComponent ? inputField.textComponent.gameObject : null,
				placeholderGameObject = inputField.placeholder ? inputField.placeholder.gameObject : null,

				asteriskChar = inputField.asteriskChar,
				caretBlinkRate = inputField.caretBlinkRate,
				customCaretColor = inputField.customCaretColor,
				hasCaretColor = caretColor.HasValue,
				caretColor = caretColor.GetValueOrDefault(),
				caretWidth = inputField.caretWidth,
				characterLimit = inputField.characterLimit,
				characterValidation = GetTMPCharacterValidation( inputField.characterValidation ),
				contentType = GetTMPContentType( inputField.contentType ),
				enabled = inputField.enabled,
				inputType = GetTMPInputType( inputField.inputType ),
				keyboardType = inputField.keyboardType,
				lineType = GetTMPLineType( inputField.lineType ),
				readOnly = inputField.readOnly,
				richText = inputField.textComponent ? inputField.textComponent.supportRichText : true, // InputField.richText overrides textComponent's Rich Text property so copy that value from the textComponent
				selectionColor = inputField.selectionColor,
				shouldHideMobileInput = inputField.shouldHideMobileInput,
				text = inputField.text,

				// Copy UnityEvents
				onEndEdit = CopyUnityEvent( inputField.onEndEdit ),
#if UNITY_5_3_OR_NEWER
				onValueChanged = CopyUnityEvent( inputField.onValueChanged )
#else
				onValueChanged = CopyUnityEvent( inputField.onValueChange )
#endif
			};
		}

		private void PasteInputFieldProperties( TMP_InputField tmp, InputFieldProperties properties )
		{
			properties.selectableProperties.ApplyTo( tmp );

			tmp.textComponent = properties.textComponentGameObject ? properties.textComponentGameObject.GetComponent<TextMeshProUGUI>() : null;
			tmp.placeholder = properties.placeholderGameObject ? properties.placeholderGameObject.GetComponent<Graphic>() : null;

			tmp.asteriskChar = properties.asteriskChar;
			tmp.caretBlinkRate = properties.caretBlinkRate;
			tmp.customCaretColor = properties.customCaretColor;
			try { if( properties.hasCaretColor ) tmp.caretColor = properties.caretColor; } catch { }
			tmp.caretWidth = Mathf.RoundToInt( properties.caretWidth );
			tmp.characterLimit = properties.characterLimit;
			tmp.characterValidation = properties.characterValidation;
			tmp.contentType = properties.contentType;
			tmp.enabled = properties.enabled;
			tmp.inputType = properties.inputType;
			tmp.keyboardType = properties.keyboardType;
			if( tmp.lineType == properties.lineType ) tmp.lineType = (TMP_InputField.LineType) ( ( (int) properties.lineType + 1 ) % 3 ); // lineType adjusts Text's enableWordWrapping setting but only if the lineType value is different
			tmp.lineType = properties.lineType;
			tmp.readOnly = properties.readOnly;
			tmp.richText = properties.richText;
			tmp.selectionColor = properties.selectionColor;
			tmp.shouldHideMobileInput = properties.shouldHideMobileInput;
			tmp.text = properties.text;

			// Paste UnityEvents
			PasteUnityEvent( tmp.onEndEdit, properties.onEndEdit );
			PasteUnityEvent( tmp.onValueChanged, properties.onValueChanged );
		}

		private TMP_Dropdown UpgradeDropdown( Dropdown dropdown, Dropdown prefabDropdown )
		{
			if( !dropdown )
				return null;

			OnComponentIsBeingUpgraded( dropdown, prefabDropdown );

			GameObject go = dropdown.gameObject;
			stringBuilder.Append( "Upgrading Dropdown: " ).AppendLine( GetPathOfObject( go.transform ) );

			// Copy fields
			Vector2 sizeDelta = ( (RectTransform) dropdown.transform ).sizeDelta;
			DropdownProperties properties = CopyDropdownProperties( dropdown );

			// Upgrade child objects
			UpgradeText( dropdown.captionText, prefabDropdown ? prefabDropdown.captionText : null );
			UpgradeText( dropdown.itemText, prefabDropdown ? prefabDropdown.itemText : null );

			// Replace Dropdown with TMP_Dropdown
			DestroyImmediate( dropdown, true );
			TMP_Dropdown tmp = go.AddComponent<TMP_Dropdown>();

			// Paste fields
			PasteDropdownProperties( tmp, properties );
			( (RectTransform) tmp.transform ).sizeDelta = sizeDelta;

			return tmp;
		}

		private DropdownProperties CopyDropdownProperties( Dropdown dropdown )
		{
			return new DropdownProperties()
			{
				gameObject = dropdown.gameObject,
				selectableProperties = new SelectableObjectProperties( dropdown ),

				template = dropdown.template,
				captionTextGameObject = dropdown.captionText ? dropdown.captionText.gameObject : null,
				itemTextGameObject = dropdown.itemText ? dropdown.itemText.gameObject : null,
				captionImage = dropdown.captionImage,
				itemImage = dropdown.itemImage,

				enabled = dropdown.enabled,
				options = GetTMPDropdownOptions( dropdown.options ),
				value = dropdown.value,

				// Copy UnityEvents
				onValueChanged = CopyUnityEvent( dropdown.onValueChanged )
			};
		}

		private void PasteDropdownProperties( TMP_Dropdown tmp, DropdownProperties properties )
		{
			properties.selectableProperties.ApplyTo( tmp );

			tmp.template = properties.template;
			tmp.captionText = properties.captionTextGameObject ? properties.captionTextGameObject.GetComponent<TextMeshProUGUI>() : null;
			tmp.itemText = properties.itemTextGameObject ? properties.itemTextGameObject.GetComponent<TextMeshProUGUI>() : null;
			tmp.captionImage = properties.captionImage;
			tmp.itemImage = properties.itemImage;

			tmp.enabled = properties.enabled;
			tmp.options = properties.options;
			tmp.value = properties.value;

			// Paste UnityEvents
			PasteUnityEvent( tmp.onValueChanged, properties.onValueChanged );
		}

		private void OnComponentIsBeingUpgraded<T>( T component, T prefabComponent ) where T : Component
		{
			if( prefabComponent )
			{
				PrefabInstancesRemovedComponent removedComponentHolder = removedComponentsInPrefabInstances.Find( ( x ) => x.component == prefabComponent );
				if( removedComponentHolder != null )
					upgradedComponentsToRemoveInPrefabInstances.Add( removedComponentHolder );
			}
		}

		private void CreateInputFieldViewport( TMP_InputField tmp )
		{
			if( !tmp.textComponent )
				return;

			RectTransform textTransform = tmp.textComponent.rectTransform;
			RectTransform placeholderTransform = tmp.placeholder ? tmp.placeholder.rectTransform : null;

			RectTransform viewport = null;
			if( textTransform.parent != tmp.transform )
			{
				viewport = (RectTransform) textTransform.parent;

				if( !viewport.GetComponent<RectMask2D>() )
					viewport.gameObject.AddComponent<RectMask2D>();
			}
			else
			{
				try
				{
					viewport = (RectTransform) new GameObject( TMP_INPUT_FIELD_TEXT_AREA_NAME, typeof( RectTransform ), typeof( RectMask2D ) ).transform;
					viewport.SetParent( tmp.transform, false );
					viewport.SetSiblingIndex( textTransform.GetSiblingIndex() );
					viewport.localPosition = textTransform.localPosition;
					viewport.localRotation = textTransform.localRotation;
					viewport.localScale = textTransform.localScale;
					viewport.anchorMin = textTransform.anchorMin;
					viewport.anchorMax = textTransform.anchorMax;
					viewport.pivot = textTransform.pivot;
					viewport.anchoredPosition = textTransform.anchoredPosition;
					viewport.sizeDelta = textTransform.sizeDelta;

					// RectMask2D.padding is reportedly added in Unity 2019.4.14: https://forum.unity.com/threads/2-1-4-does-not-contain-a-definition-for-padding.1064537/
#if UNITY_2019_4_OR_NEWER && !UNITY_2019_4_1 && !UNITY_2019_4_2 && !UNITY_2019_4_3 && !UNITY_2019_4_4 && !UNITY_2019_4_5 && !UNITY_2019_4_6 && !UNITY_2019_4_7 && !UNITY_2019_4_8 && !UNITY_2019_4_9 && !UNITY_2019_4_10 && !UNITY_2019_4_11 && !UNITY_2019_4_12 && !UNITY_2019_4_13
					viewport.GetComponent<RectMask2D>().padding = new Vector4( 2f - viewport.offsetMin.x, 1f - viewport.offsetMin.y, 2f + viewport.offsetMax.x, 2f + viewport.offsetMax.y );
#endif

#if UNITY_2018_3_OR_NEWER
					PrefabUtility.RecordPrefabInstancePropertyModifications( viewport.gameObject );
					PrefabUtility.RecordPrefabInstancePropertyModifications( viewport );
#endif

					for( int i = tmp.transform.childCount - 1; i >= 0; i-- )
					{
						RectTransform child = tmp.transform.GetChild( i ) as RectTransform;
						if( !child || child == viewport )
							continue;

						if( child == textTransform || child == placeholderTransform )
						{
							child.SetParent( viewport, true );

							// SetParent can fail if InputField is an instance of a prefab in the scene and the prefab asset isn't upgraded (only the scene is upgraded)
							if( child.parent == viewport )
							{
								child.SetSiblingIndex( 0 );

								// If child's anchors are separated, set them to min=(0,0) and max=(1,1) so that they envelop the viewport
								if( ( child.anchorMax - child.anchorMin ).sqrMagnitude > Mathf.Epsilon )
								{
									// See: https://github.com/Unity-Technologies/UnityCsReference/blob/33cbfe062d795667c39e16777230e790fcd4b28b/Editor/Mono/Inspector/RectTransformEditor.cs#L1272-L1275
									// See: https://github.com/Unity-Technologies/UnityCsReference/blob/73c12b5a403abad9a300f01a81e7aaf30a0d30b5/Editor/Mono/Inspector/LayoutDropdownWindow.cs#L343-L348
									if( rectTransformAnchorsSetterMethod == null )
										rectTransformAnchorsSetterMethod = typeof( Editor ).Assembly.GetType( "UnityEditor.RectTransformEditor" ).GetMethod( "SetAnchorSmart", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof( RectTransform ), typeof( float ), typeof( int ), typeof( bool ), typeof( bool ), typeof( bool ) }, null );

									rectTransformAnchorsSetterMethod.Invoke( null, new object[] { child, 0, 0, false, true, true } );
									rectTransformAnchorsSetterMethod.Invoke( null, new object[] { child, 1, 0, true, true, true } );
									rectTransformAnchorsSetterMethod.Invoke( null, new object[] { child, 0, 1, false, true, true } );
									rectTransformAnchorsSetterMethod.Invoke( null, new object[] { child, 1, 1, true, true, true } );
								}

#if UNITY_2018_3_OR_NEWER
								PrefabUtility.RecordPrefabInstancePropertyModifications( child );
#endif
							}
						}
					}
				}
				catch
				{
					if( viewport )
					{
						DestroyImmediate( viewport.gameObject );
						viewport = null;
					}

					throw;
				}
			}

			tmp.textViewport = viewport;
		}

		private void ApplyPrefabInstanceModifications<LegacyComponentType, UpgradedComponentType>( ComponentProperties<LegacyComponentType, UpgradedComponentType> properties, Action<UpgradedComponentType> applyModificationsFunction, ref int progressCurrent, int progressTotal ) where LegacyComponentType : Component where UpgradedComponentType : Component
		{
			EditorUtility.DisplayProgressBar( "Applying prefab instance modifications...", properties.gameObject ? GetPathOfObject( properties.gameObject.transform ) : "Missing", (float) progressCurrent / progressTotal );
			progressCurrent++;

			if( !properties.gameObject )
			{
				stringBuilder.Append( "<b>Prefab instance with " ).Append( typeof( LegacyComponentType ).Name ).AppendLine( " modifications is missing</b>" );
				return;
			}

			UpgradedComponentType tmp = properties.gameObject.GetComponent<UpgradedComponentType>();
			if( !tmp )
			{
				stringBuilder.Append( "<b>Prefab instance with modifications is missing its upgraded " ).Append( typeof( UpgradedComponentType ).Name ).AppendLine( " component</b>" );
				return;
			}

			stringBuilder.Append( "Applying prefab instance modifications to upgraded component: " ).Append( GetPathOfObject( tmp.transform ) ).Append( "." ).AppendLine( typeof( UpgradedComponentType ).Name );
			applyModificationsFunction( tmp );

			EditorUtility.SetDirty( tmp );
		}

		private List<TMP_Dropdown.OptionData> GetTMPDropdownOptions( List<Dropdown.OptionData> options )
		{
			if( options == null )
				return null;

			List<TMP_Dropdown.OptionData> result = new List<TMP_Dropdown.OptionData>( options.Count );
			for( int i = 0; i < options.Count; i++ )
				result.Add( new TMP_Dropdown.OptionData( options[i].text, options[i].image ) );

			return result;
		}

		private TMP_InputField.CharacterValidation GetTMPCharacterValidation( InputField.CharacterValidation characterValidation )
		{
			switch( characterValidation )
			{
				case InputField.CharacterValidation.Alphanumeric: return TMP_InputField.CharacterValidation.Alphanumeric;
				case InputField.CharacterValidation.Decimal: return TMP_InputField.CharacterValidation.Decimal;
				case InputField.CharacterValidation.EmailAddress: return TMP_InputField.CharacterValidation.EmailAddress;
				case InputField.CharacterValidation.Integer: return TMP_InputField.CharacterValidation.Integer;
				case InputField.CharacterValidation.Name: return TMP_InputField.CharacterValidation.Name;
				case InputField.CharacterValidation.None: return TMP_InputField.CharacterValidation.None;
				default: return TMP_InputField.CharacterValidation.None;
			}
		}

		private TMP_InputField.ContentType GetTMPContentType( InputField.ContentType contentType )
		{
			switch( contentType )
			{
				case InputField.ContentType.Alphanumeric: return TMP_InputField.ContentType.Alphanumeric;
				case InputField.ContentType.Autocorrected: return TMP_InputField.ContentType.Autocorrected;
				case InputField.ContentType.Custom: return TMP_InputField.ContentType.Custom;
				case InputField.ContentType.DecimalNumber: return TMP_InputField.ContentType.DecimalNumber;
				case InputField.ContentType.EmailAddress: return TMP_InputField.ContentType.EmailAddress;
				case InputField.ContentType.IntegerNumber: return TMP_InputField.ContentType.IntegerNumber;
				case InputField.ContentType.Name: return TMP_InputField.ContentType.Name;
				case InputField.ContentType.Password: return TMP_InputField.ContentType.Password;
				case InputField.ContentType.Pin: return TMP_InputField.ContentType.Pin;
				case InputField.ContentType.Standard: return TMP_InputField.ContentType.Standard;
				default: return TMP_InputField.ContentType.Standard;
			}
		}

		private TMP_InputField.InputType GetTMPInputType( InputField.InputType inputType )
		{
			switch( inputType )
			{
				case InputField.InputType.AutoCorrect: return TMP_InputField.InputType.AutoCorrect;
				case InputField.InputType.Password: return TMP_InputField.InputType.Password;
				case InputField.InputType.Standard: return TMP_InputField.InputType.Standard;
				default: return TMP_InputField.InputType.Standard;
			}
		}

		private TMP_InputField.LineType GetTMPLineType( InputField.LineType lineType )
		{
			switch( lineType )
			{
				case InputField.LineType.MultiLineNewline: return TMP_InputField.LineType.MultiLineNewline;
				case InputField.LineType.MultiLineSubmit: return TMP_InputField.LineType.MultiLineSubmit;
				case InputField.LineType.SingleLine: return TMP_InputField.LineType.SingleLine;
				default: return TMP_InputField.LineType.SingleLine;
			}
		}

		private TextAlignmentOptions GetTMPAlignment( TextAnchor alignment, bool alignByGeometry )
		{
			switch( alignment )
			{
				case TextAnchor.LowerLeft: return alignByGeometry ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.BottomLeft;
				case TextAnchor.LowerCenter: return alignByGeometry ? TextAlignmentOptions.BottomGeoAligned : TextAlignmentOptions.Bottom;
				case TextAnchor.LowerRight: return alignByGeometry ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.BottomRight;
				case TextAnchor.MiddleLeft: return alignByGeometry ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Left;
				case TextAnchor.MiddleCenter: return alignByGeometry ? TextAlignmentOptions.MidlineGeoAligned : TextAlignmentOptions.Center;
				case TextAnchor.MiddleRight: return alignByGeometry ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.Right;
				case TextAnchor.UpperLeft: return alignByGeometry ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopLeft;
				case TextAnchor.UpperCenter: return alignByGeometry ? TextAlignmentOptions.TopGeoAligned : TextAlignmentOptions.Top;
				case TextAnchor.UpperRight: return alignByGeometry ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopRight;
				default: return alignByGeometry ? TextAlignmentOptions.MidlineGeoAligned : TextAlignmentOptions.Center;
			}
		}

		private FontStyles GetTMPFontStyle( FontStyle fontStyle )
		{
			switch( fontStyle )
			{
				case FontStyle.Bold: return FontStyles.Bold;
				case FontStyle.Italic: return FontStyles.Italic;
				case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
				default: return FontStyles.Normal;
			}
		}

		private TextOverflowModes GetTMPVerticalOverflow( VerticalWrapMode verticalOverflow, HorizontalWrapMode horizontalOverflow )
		{
			if( alwaysUseOverflowForNonWrappingTexts && horizontalOverflow == HorizontalWrapMode.Overflow )
				return TextOverflowModes.Overflow;

			return verticalOverflow == VerticalWrapMode.Overflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
		}

		private object CopyUnityEvent( UnityEventBase target )
		{
			if( unityEventPersistentCallsField == null )
			{
				unityEventPersistentCallsField = typeof( UnityEventBase ).GetField( "m_PersistentCalls", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
				if( unityEventPersistentCallsField == null )
				{
					unityEventPersistentCallsField = typeof( UnityEventBase ).GetField( "m_PersistentListeners", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

					if( unityEventPersistentCallsField == null )
					{
						stringBuilder.AppendLine( "<b>Couldn't copy UnityEvent</b>" );
						return null;
					}
				}
			}

			return unityEventPersistentCallsField.GetValue( target );
		}

		private void PasteUnityEvent( UnityEventBase target, object unityEvent )
		{
			if( unityEvent != null )
				unityEventPersistentCallsField.SetValue( target, unityEvent );
			else
				stringBuilder.AppendLine( "<b>Couldn't paste UnityEvent because it became null (it can happen on Unity 2019.2 or earlier if a script was modified during the upgrade process)</b>" );
		}
	}
}