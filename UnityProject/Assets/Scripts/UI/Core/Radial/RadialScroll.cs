using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Core.Radial
{
	public enum ScrollEventArea
	{
		None,
		Annulus,
		FullRadius,
		Screen
	}

	[RequireComponent(typeof(IRadial))]
	public class RadialScroll : MonoBehaviour, IScrollHandler
	{
		[Tooltip("The number of items to scroll when using the mouse wheel.")]
		[SerializeField]
		private int scrollCount;

		[Tooltip("The area that the mouse pointer must be inside in order to trigger a mouse wheel event.")]
		[SerializeField]
		private ScrollEventArea wheelEventArea;

		[Tooltip("The area that the mouse pointer must be inside in order to trigger a key event.")]
		[SerializeField]
		private ScrollEventArea keyEventArea;

		private PointerEventData PointerEvent { get; } = new PointerEventData(EventSystem.current);

		private IRadial RadialUI { get; set; }

		public Action<PointerEventData> OnScrollEvent { get; set; }

		public Action<PointerEventData> OnKeyEvent { get; set; }

		public int ScrollCount
		{
			get => scrollCount;
			set => scrollCount = value;
		}

		public ScrollEventArea WheelEventArea
		{
			get => wheelEventArea;
			set => wheelEventArea = value;
		}

		public ScrollEventArea KeyEventArea
		{
			get => keyEventArea;
			set => keyEventArea = value;
		}

		public void Awake()
		{
			RadialUI = GetComponent<IRadial>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDestroy()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public void OnScroll(PointerEventData eventData)
		{
			if (eventData.scrollDelta == Vector2.zero || IsPositionInArea(eventData.position, WheelEventArea) == false)
			{
				return;
			}

			var scrollDelta = eventData.scrollDelta;
			scrollDelta.y = GetScrollDelta(scrollDelta.y < 0);
			eventData.scrollDelta = scrollDelta;

			OnScrollEvent?.Invoke(eventData);
		}

		private bool IsPositionInArea(Vector3 position, ScrollEventArea eventArea)
		{
			if (eventArea == ScrollEventArea.None) return false;
			return eventArea == ScrollEventArea.Screen
				|| RadialUI.IsPositionWithinRadial(position, eventArea == ScrollEventArea.FullRadius);
		}

		private float GetScrollDelta(bool forward)
		{
			var delta = RadialUI.ItemArcMeasure * scrollCount;
			return forward ? -delta : delta;
		}

		private void UpdateMe()
		{
			var mousePos = CommonInput.mousePosition;

			if (IsPositionInArea(mousePos, KeyEventArea) == false) return;

			var keyManager = KeyboardInputManager.Instance;
			var forward = keyManager.CheckKeyAction(KeyAction.RadialScrollForward);
			var backward = keyManager.CheckKeyAction(KeyAction.RadialScrollBackward);

			if (forward == false && backward == false) return;

			var eventData = PointerEvent;
			eventData.pointerId = 0;
			eventData.position = mousePos;
			eventData.scrollDelta = new Vector2(0, GetScrollDelta(forward));

			OnKeyEvent?.Invoke(eventData);
		}
	}
}