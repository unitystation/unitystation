using Animations.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using US13.UI.Core;

namespace US13.UI.Systems.MainHUD.TabDropdown
{
	/// <summary>
	///     Custom ResizePanel for the PANEL_Right UI element
	/// </summary>
	public class RightPanelResize : ResizePanel
	{
		private float hudRight_dist;
		private float leftRange;
		public RectTransform panelRight;
		public ResponsiveUI responsiveControl;
		public GameObject returnPanelButton;

		public override void OnPointerDown(PointerEventData data)
		{
			base.OnPointerDown(data);
		}

		//TODO showing the transparent chatbox when panel is hidden
		public override void OnDrag(PointerEventData data)
		{
		}
	}
}
