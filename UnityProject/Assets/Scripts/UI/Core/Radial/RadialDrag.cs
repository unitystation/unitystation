using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Core.Radial
{
	[RequireComponent(typeof(IRadial))]
	public class RadialDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		[Tooltip("The drag speed multiplier.")]
		[SerializeField]
		private float speedFactor = 0.1f;

		[Tooltip("The mouse drag delta required to trigger the drag event.")]
		[SerializeField]
		private float dragThreshold = default;

		[Tooltip("The max drag delta to allow.")]
		[SerializeField]
		private float deltaClamp = default;

		private IRadial RadialUI { get; set; }

		public Action<PointerEventData> OnBeginDragEvent { get; set;  }

		public Action<PointerEventData> OnDragEvent { get; set; }

		public Action<PointerEventData> OnEndDragEvent { get; set; }

		public void Awake()
		{
			RadialUI = GetComponent<IRadial>();
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			OnBeginDragEvent?.Invoke(eventData);
			UIManager.IsMouseInteractionDisabled = true;
		}

		public void OnDrag(PointerEventData eventData)
		{
			var delta = eventData.delta;
			var relativePosition = (Vector2)transform.position - eventData.position;
			var theta = Mathf.Rad2Deg * Mathf.Atan2(relativePosition.y, relativePosition.x);
			var fixedDelta = delta.RotateAroundZ(Vector3.zero, (360 - theta) % 360);
			var rotationAmount = delta.sqrMagnitude * Mathf.Sign(fixedDelta.y) * speedFactor;
			rotationAmount = Mathf.Clamp(rotationAmount, -deltaClamp, deltaClamp);
			if (Math.Abs(rotationAmount) < dragThreshold)
			{
				return;
			}
			RadialUI.RotateRadial(-rotationAmount);
			eventData.delta = fixedDelta;
			OnDragEvent?.Invoke(eventData);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			UIManager.IsMouseInteractionDisabled = false;
			OnEndDragEvent?.Invoke(eventData);
		}

		public void OnDisable()
		{
			UIManager.IsMouseInteractionDisabled = false;
		}
	}
}
