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

		private Image mask;

		private RightClickButton button;

		private Image Mask => this.GetComponentByRef(ref mask);

		private RightClickButton Button => this.GetComponentByRef(ref button);

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
		}

		public void SetInteractable(bool value)
		{
			if (mask.raycastTarget)
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
			Mask.CrossFadeColor(color * colors.colorMultiplier, instant ? 0 : colors.fadeDuration, false, true);
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

		public void ScaleIcon(float scale)
		{
			icon.rectTransform.localScale = new Vector2(scale, scale);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			var selected = Radial.Selected;
			if (selected != this)
			{
				selected.OrNull()?.FadeOut(eventData);
				Radial.Selected = this;
			}

			if (eventData.dragging || Button.interactable == false)
			{
				return;
			}
			SetColor(colors.highlightedColor);
			Button.OnPointerEnter(eventData);
			Radial.Invoke(PointerEventType.PointerEnter, eventData, this);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Radial.Invoke(PointerEventType.PointerExit, eventData, this);
		}

		public void OnPointerClick(PointerEventData eventData) =>
			Radial.Invoke(PointerEventType.PointerClick, eventData, this);
	}
}
