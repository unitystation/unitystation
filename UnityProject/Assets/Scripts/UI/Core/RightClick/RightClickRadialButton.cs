using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UI.Core.Radial;
using UI.Core.Events;

namespace UI.Core.RightClick
{
	public class RightClickRadialButton : RadialItem<RightClickRadialButton>, IPointerEnterHandler, IPointerClickHandler, IBeginDragHandler, ISelectHandler
	{
		private static readonly int IsPaletted = Shader.PropertyToID("_IsPaletted");
		private static readonly int ColorPalette = Shader.PropertyToID("_ColorPalette");

		[SerializeField]
		private Button button = default;

		[SerializeField]
		private Image icon = default;

		[SerializeField]
		private Color iconFadedColor = default;

		[SerializeField]
		private float iconFadeDuration = default;

		[SerializeField]
		private RectTransform divider = default;

	    private Image Mask { get; set; }

	    protected void Awake()
	    {
		    Mask = GetComponent<Image>();
		    button = GetComponent<Button>();
	    }

	    public override void Setup(Radial<RightClickRadialButton> parent, int index)
	    {
		    base.Setup(parent, index);
	        Mask.fillAmount = 1f / 360f * parent.ItemArcAngle;
	        var iconTransform = icon.transform;
	        iconTransform.localPosition = Radial.ItemCenter;
	        iconTransform.localScale = new Vector3(Radial.Scale, Radial.Scale, 1f);
	    }

	    public void SetDividerActive(bool active) => divider.SetActive(active);

	    public void LateUpdate()
	    {
		    icon.transform.rotation = Quaternion.identity;
	    }

	    public void ChangeItem(RightClickMenuItem itemInfo)
	    {
		    Mask.color = itemInfo.BackgroundColor;
		    var colors = button.colors;
		    colors.highlightedColor = CalculateHighlight(itemInfo.BackgroundColor);
		    colors.selectedColor = colors.highlightedColor;
		    button.colors = colors;
		    icon.canvasRenderer.SetColor(iconFadedColor);
		    icon.color = itemInfo.IconColor;
		    icon.sprite = itemInfo.IconSprite;
		    var palette = itemInfo.palette;
		    if (palette != null)
		    {
			    icon.material.SetInt(IsPaletted, 1);
			    icon.material.SetColorArray(ColorPalette, palette.ToArray());
		    }
		    else
		    {
			    icon.material.SetInt(IsPaletted, 0);
		    }
	    }

	    private Color CalculateHighlight(Color original)
	    {
		    return new Color(CalcChannel(original.r), CalcChannel(original.g), CalcChannel(original.b), 1f);

		    float CalcChannel(float channel) => channel >= 0.5f
			    ? Mathf.Lerp(channel, 0, (channel - 0.5f) * 0.5f)
			    : Mathf.Lerp(channel, 1, (0.5f - channel) * 0.5f);
	    }

	    public void OnPointerEnter(PointerEventData eventData)
	    {
		    if (eventData.dragging)
		    {
			    return;
		    }
		    OnSelect(eventData);
		    Radial.Invoke(PointerEventType.PointerEnter, eventData, this);
	    }

	    public void OnPointerClick(PointerEventData eventData) =>
			Radial.Invoke(PointerEventType.PointerClick, eventData, this);

	    public void OnBeginDrag(PointerEventData eventData) => OnDeselect(eventData);

	    public void OnSelect(BaseEventData eventData)
	    {
		    Radial.Selected.OrNull()?.OnDeselect(eventData);
		    Radial.Selected = this;
		    icon.CrossFadeColor(Color.white, iconFadeDuration, false, true);
	    }

	    public void OnDeselect(BaseEventData eventData)
	    {
		    // Leaving the IOnDeselectHandler out so that this OnDeselect isn't triggered when an action radial or other button is selected/pressed.
		    Radial.Selected = null;
		    button.OnDeselect(eventData);
		    icon.CrossFadeColor(iconFadedColor, iconFadeDuration, false, true);
	    }
	}
}
