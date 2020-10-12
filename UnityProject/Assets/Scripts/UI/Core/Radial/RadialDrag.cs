using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI.Core.Radial
{
    [RequireComponent(typeof(IRadial))]
    public class RadialDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public class DragEvent : UnityEvent<PointerEventData> {}

        [Tooltip("The drag speed multiplier.")]
        [SerializeField]
        private float speedFactor = 0.1f;

        [Tooltip("The mouse drag delta required to trigger the drag event.")]
        [SerializeField]
        private float dragThreshold = default;

        private IRadial RadialUI { get; set; }

        public DragEvent OnBeginDragEvent { get; } = new DragEvent();

        public DragEvent OnDragEvent { get; } = new DragEvent();

        public DragEvent OnEndDragEvent { get; } = new DragEvent();

        public void Awake()
        {
            RadialUI = GetComponent<IRadial>();
        }

        public void OnBeginDrag(PointerEventData eventData) => OnBeginDragEvent.Invoke(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            var delta = eventData.delta;
            var mousePos = eventData.position;
            var radialPos = transform.position;
            var theta = Mathf.Rad2Deg * Mathf.Atan2(radialPos.y - mousePos.y, radialPos.x - mousePos.x);
            var fixedDelta = delta.RotateAroundZ(Vector3.zero, (360 - theta) % 360);
            var rotationAmount = delta.sqrMagnitude * Mathf.Sign(fixedDelta.y) * speedFactor;
            var halfPI = Mathf.PI / 2;
            rotationAmount = Mathf.Clamp(rotationAmount, -RadialUI.ItemArcMeasure / halfPI, RadialUI.ItemArcMeasure / halfPI);
            if (Math.Abs(rotationAmount) < dragThreshold)
            {
	            return;
            }
            RadialUI.RotateRadial(-rotationAmount);
            OnDragEvent.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData) => OnEndDragEvent.Invoke(eventData);

    }
}
