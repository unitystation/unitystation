using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UI.Core.Radial;
using UI.Core.Events;

namespace UI.Core.RightClick
{
	public class RightClickRadialButton : RadialItem<RightClickRadialButton>, IPointerEnterHandler, IPointerClickHandler
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
		private Color iconFadedColor = default;

		[SerializeField]
		private float iconFadeDuration = default;

		[SerializeField]
		private RectTransform divider = default;

		private Image mask;

		private RightClickButton button;

		private ColorBlock buttonColors = ColorBlock.defaultColorBlock;

		private T InternalGetComponent<T>(ref T obj)
		{
			if (obj == null)
			{
				obj = GetComponent<T>();
			}

			return obj;
		}

		private Image Mask => InternalGetComponent(ref mask);

		private RightClickButton Button => InternalGetComponent(ref button);

		private ColorBlock ButtonColors
		{
			get => buttonColors;
			set
			{
				buttonColors = value;
				Button.colors = value;
			}
		}

		public override void Setup(Radial<RightClickRadialButton> parent, int index)
		{
			base.Setup(parent, index);
			Mask.fillAmount = 1f / 360f * parent.ItemArcMeasure;
			var iconTransform = icon.rectTransform;
			iconTransform.localPosition = Radial.ItemCenter;
			iconTransform.localScale = Vector3.one;
		}

		public void SetInteractable(bool value) => Button.interactable = value;

		public void SetDividerActive(bool active) => divider.SetActive(active);

		public void LateUpdate()
		{
			icon.transform.rotation = Quaternion.identity;
		}

		public void ChangeItem(RightClickMenuItem itemInfo)
		{
			Mask.raycastTarget = true;
			Mask.color = itemInfo.BackgroundColor;
			var colors = ButtonColors;
			colors.highlightedColor = CalculateHighlight(itemInfo.BackgroundColor);
			colors.selectedColor = colors.highlightedColor;
			colors.disabledColor = colors.normalColor;
			ButtonColors = colors;
			icon.canvasRenderer.SetColor(iconFadedColor);
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

		public void RemoveItem()
		{
			// Dragging needs to be able to set button interactivity and its disabled color needs to be the same as the
			// normal color. Using this to disable the button and set transparency. ChangeItem will reactivate it.
			Mask.raycastTarget = false;
			Button.colors = DisabledColors;
			icon.color = Color.clear;
			icon.sprite = null;
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
			icon.CrossFadeColor(iconFadedColor, 0, true, true);
		}

		public void FadeOut(BaseEventData eventData)
		{
			Button.OnDeselect(eventData);
			icon.CrossFadeColor(iconFadedColor, iconFadeDuration, false, true);
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
			icon.CrossFadeColor(Color.white, iconFadeDuration, false, true);
			Button.OnPointerEnter(eventData);
			Radial.Invoke(PointerEventType.PointerEnter, eventData, this);
		}

		public void OnPointerClick(PointerEventData eventData) =>
			Radial.Invoke(PointerEventType.PointerClick, eventData, this);
	}
}
