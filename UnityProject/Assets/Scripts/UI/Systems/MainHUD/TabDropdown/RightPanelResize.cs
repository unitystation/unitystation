using UnityEngine;
using UnityEngine.EventSystems;


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
