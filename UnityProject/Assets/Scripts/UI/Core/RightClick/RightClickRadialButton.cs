using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UI.Core.Radial;
using UI.Core.Events;

namespace UI.Core.RightClick
{
	public class RightClickRadialButton : RadialItem<RightClickRadialButton>, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		private static readonly int IsPaletted = Shader.PropertyToID("_IsPaletted");
		private static readonly int PaletteSize = Shader.PropertyToID("_PaletteSize");
		private static readonly int ColorPalette = Shader.PropertyToID("_ColorPalette");

		private static readonly ColorBlock DisabledColors = new ColorBlock
		{
			normalColor = Color.clear,
			highlightedColor = Color.clear,
			selectedColor = Color.clear,
			disabledColor = Color.clear
		};

		[SerializeField]
		private Image icon = default;

		[SerializeField]
		private TMP_Text altLabel = default;

		[SerializeField]
		private ColorBlock colors = ColorBlock.defaultColorBlock;

		[SerializeField]
		private RectTransform divider = default;

		[Min(0)]
		[SerializeField]
		private float selectionDelay = default;

		private Image mask;

		private RightClickButton button;

		private Image Mask => this.GetComponentByRef(ref mask);

		private RightClickButton Button => this.GetComponentByRef(ref button);

		private int SelectionDelayId { get; set; }

		private System.Action SelectionDelegate { get; set; }

		private System.Action<Color> UpdateColorDelegate { get; set; }

		private PointerEventData LastPointerEnterData { get; set; }

		private float IconScale { get; set; }

		private void Awake()
		{
			icon.material = Instantiate(icon.material);
			SelectionDelegate = Select;
			UpdateColorDelegate = UpdateColor;
		}

		public override void Setup(Radial<RightClickRadialButton> parent, int index)
		{
			base.Setup(parent, index);
			Mask.fillAmount = 1f / 360f * parent.ItemArcMeasure;
			var iconTransform = icon.rectTransform;
			iconTransform.localPosition = Radial.ItemCenter;
			iconTransform.localScale = Vector3.one;
			var altLabelTransform = altLabel.transform;
			altLabelTransform.localPosition = Radial.ItemCenter;
			altLabelTransform.localScale = Vector3.one;
			SetInteractable(true);
		}

		public void SetInteractable(bool value)
		{
			if (mask.raycastTarget && Button.interactable != value)
			{
				Button.interactable = value;
				Mask.canvasRenderer.SetColor(colors.normalColor * colors.colorMultiplier);
			}
		}

		public void SetDividerActive(bool active) => divider.SetActive(active);

		public void LateUpdate()
		{
			icon.transform.rotation = Quaternion.identity;
			altLabel.transform.rotation = Quaternion.identity;
		}

		public void ChangeItem(RightClickMenuItem itemInfo)
		{
			Mask.raycastTarget = true;
			Mask.color = itemInfo.BackgroundColor;
			// Due to the way Unity handles selectables and transitions, we will handle the transitions ourselves instead.
			var colorBlock = colors;
			colorBlock.highlightedColor = CalculateHighlight(itemInfo.BackgroundColor);
			colorBlock.selectedColor = colorBlock.highlightedColor;
			colorBlock.disabledColor = Color.clear;
			colors = colorBlock;
			// Temporary solution for items/actions that currently do not have an icon set up or have the default question mark icon.
			if (itemInfo.IconSprite == null || itemInfo.IconSprite.name == "question_mark")
			{
				icon.SetActive(false);
				altLabel.SetActive(true);
				altLabel.SetText(itemInfo.Label);
			}
			else
			{
				SetupIcon(itemInfo);
			}
		}

		private void SetupIcon(RightClickMenuItem itemInfo)
		{
			icon.SetActive(true);
			altLabel.SetActive(false);
			icon.color = itemInfo.IconColor;
			icon.sprite = itemInfo.IconSprite;
			var palette = itemInfo.palette;
			if (palette != null)
			{
				icon.material.SetInt(IsPaletted, 1);
				icon.material.SetInt(PaletteSize, palette.Count);
				icon.material.SetColorArray(ColorPalette, palette.ToArray());
			}
			else
			{
				icon.material.SetInt(IsPaletted, 0);
			}

			var spriteMetadata = icon.ApplySpriteScaling(itemInfo.IconSprite);
			IconScale = spriteMetadata.Scale;
		}

		public void DisableItem()
		{
			Mask.raycastTarget = false;
			SetColor(colors.disabledColor, true);
			icon.color = Color.clear;
			icon.sprite = null;
			altLabel.SetText(string.Empty);
		}

		private void SetColor(Color color, bool instant = false)
		{
			var maskRenderer = Mask.canvasRenderer;
			LeanTween.cancel(gameObject);
			if (instant)
			{
				maskRenderer.SetColor(color);
				return;
			}
			var duration = colors.fadeDuration;
			var fromColor = maskRenderer.GetColor();
			var toColor = color * colors.colorMultiplier;
			LeanTween.value(gameObject, UpdateColorDelegate, fromColor, toColor, instant ? 0 : duration);
		}

		private void UpdateColor(Color color)
		{
			Mask.canvasRenderer.SetColor(color);
		}

		private Color CalculateHighlight(Color original)
		{
			return new Color(CalcChannel(original.r), CalcChannel(original.g), CalcChannel(original.b), 1f);

			float CalcChannel(float channel) => channel >= 0.5f
				? Mathf.Lerp(channel, 0, (channel - 0.5f) * 0.5f)
				: Mathf.Lerp(channel, 1, (0.5f - channel) * 0.5f);
		}

		public void ResetState()
		{
			Button.ResetState();
			SetColor(colors.normalColor, true);
		}

		public void FadeOut(BaseEventData eventData)
		{
			Button.OnDeselect(eventData);
			SetColor(colors.normalColor);
		}

		public void ScaleIcon(float scale, bool shrink)
		{
			var iconTransform = icon.transform;
			float scaleMultiplier;
			if (shrink)
			{
				if (Mathf.Round(scale * 100) / 100 < 1)
				{
					scaleMultiplier = LeanTween.easeOutCirc(1, 0, scale);
				}
				else
				{
					scaleMultiplier = 1;
				}
			}
			else
			{
				scaleMultiplier = LeanTween.easeInCirc(0, 1, scale);
			}
			iconTransform.localScale = new Vector3(scaleMultiplier * IconScale, scaleMultiplier * IconScale, 1);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			LastPointerEnterData = eventData;
			var hasDelay = RightClickMenuController.RadialOptions.ShowActionRadial;

			if (Radial.Selected is null || selectionDelay < 0.01f || hasDelay == false)
			{
				Select();
			}
			else
			{
				SelectionDelayId = LeanTween.delayedCall(selectionDelay, SelectionDelegate).id;
			}
		}

		private void Select()
		{
			if (LastPointerEnterData == null || Radial.isActiveAndEnabled == false)
			{
				return;
			}

			var selected = Radial.Selected;
			if (selected != this)
			{
				selected.OrNull()?.FadeOut(LastPointerEnterData);
				Radial.Selected = this;
			}

			if (LastPointerEnterData.dragging || Button.interactable == false)
			{
				return;
			}

			SetColor(colors.highlightedColor);
			Button.OnPointerEnter(LastPointerEnterData);
			Radial.Invoke(PointerEventType.PointerEnter, LastPointerEnterData, this);
			SelectionDelayId = 0;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			LeanTween.cancel(SelectionDelayId);
			SelectionDelayId = 0;
			Radial.Invoke(PointerEventType.PointerExit, eventData, this);
		}

		public void OnPointerClick(PointerEventData eventData) =>
			Radial.Invoke(PointerEventType.PointerClick, eventData, this);
	}
}
