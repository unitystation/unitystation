using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TextToTMPNamespace
{
	public partial class TextToTMPWindow
	{
		private abstract class ComponentProperties<LegacyComponentType, UpgradedComponentType> where LegacyComponentType : Component where UpgradedComponentType : Component
		{
			public GameObject gameObject;
		}

		[Serializable]
		private class TextProperties : ComponentProperties<Text, TextMeshProUGUI>
		{
			public TextAlignmentOptions alignment;
			public bool bestFit;
			public int bestFitMaxSize;
			public int bestFitMinSize;
			public Color color;
			public bool enabled;
			public Material fontMaterial;
			public TMP_FontAsset font;
			public int fontSize;
			public FontStyles fontStyle;
			public bool horizontalWrapMode;
			public float lineSpacing;
			public bool raycastTarget;
			public bool supportRichText;
			public string text;
			public TextOverflowModes verticalOverflow;
		}

		[Serializable]
		private class TextMeshProperties : ComponentProperties<TextMesh, TextMeshPro>
		{
			public TextAlignmentOptions alignment;
			public float characterSize;
			public Color color;
			public Material fontMaterial;
			public TMP_FontAsset font;
			public int fontSize;
			public FontStyles fontStyle;
			public float lineSpacing;
			public float offsetZ;
			public bool richText;
			public string text;
		}

		[Serializable]
		private class InputFieldProperties : ComponentProperties<InputField, TMP_InputField>
		{
			public SelectableObjectProperties selectableProperties;

			public GameObject textComponentGameObject; // See SelectableObjectProperties.targetGraphicGameObject for the reason that this is a GameObject
			public GameObject placeholderGameObject;

			public char asteriskChar;
			public float caretBlinkRate;
			public bool customCaretColor;
			public bool hasCaretColor;
			public Color caretColor;
			public float caretWidth;
			public int characterLimit;
			public TMP_InputField.CharacterValidation characterValidation;
			public TMP_InputField.ContentType contentType;
			public bool enabled;
			public TMP_InputField.InputType inputType;
			public TouchScreenKeyboardType keyboardType;
			public TMP_InputField.LineType lineType;
			public bool readOnly;
			public bool richText;
			public Color selectionColor;
			public bool shouldHideMobileInput;
			public string text;

			// UnityEvents
#if UNITY_2019_3_OR_NEWER
			[SerializeReference]
#endif
			public object onEndEdit;
#if UNITY_2019_3_OR_NEWER
			[SerializeReference]
#endif
			public object onValueChanged;
		}

		[Serializable]
		private class DropdownProperties : ComponentProperties<Dropdown, TMP_Dropdown>
		{
			public SelectableObjectProperties selectableProperties;

			public RectTransform template;
			public GameObject captionTextGameObject; // See SelectableObjectProperties.targetGraphicGameObject for the reason that this is a GameObject
			public GameObject itemTextGameObject;
			public Image captionImage;
			public Image itemImage;

			public bool enabled;
			public List<TMP_Dropdown.OptionData> options;
			public int value;

			// UnityEvents
#if UNITY_2019_3_OR_NEWER
			[SerializeReference]
#endif
			public object onValueChanged;
		}

		[Serializable]
		private class SelectableObjectProperties
		{
			public AnimationTriggers animationTriggers;
			public Image image;
			public ColorBlock colors;
			public bool interactable;
			public Navigation navigation;
			public SpriteState spriteState;
			public GameObject targetGraphicGameObject; // Storing it as GameObject because if the Graphic points to a Text, then it will be converted to a TextMeshProUGUI and the only way we don't lose that reference is by storing the GameObject rather than the Graphic
			public Selectable.Transition transition;

			public SelectableObjectProperties( Selectable selectable )
			{
				animationTriggers = selectable.animationTriggers;
				image = selectable.image;
				colors = selectable.colors;
				interactable = selectable.interactable;
				navigation = selectable.navigation;
				spriteState = selectable.spriteState;
				targetGraphicGameObject = selectable.targetGraphic ? selectable.targetGraphic.gameObject : null;
				transition = selectable.transition;
			}

			public void ApplyTo( Selectable selectable )
			{
				selectable.animationTriggers = animationTriggers;
				selectable.image = image;
				selectable.colors = colors;
				selectable.interactable = interactable;
				selectable.navigation = navigation;
				selectable.spriteState = spriteState;
				selectable.targetGraphic = targetGraphicGameObject ? targetGraphicGameObject.GetComponent<Graphic>() : null;
				selectable.transition = transition;
			}
		}
	}
}