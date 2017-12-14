using UnityEngine;
using UnityEngine.EventSystems;

namespace Light2D.Examples
{
    public class UICustomSlider : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Vector2 _maxPos;
        [SerializeField] private Vector2 _minPos;

        public float Value { get; private set; }

        public void OnDrag(PointerEventData eventData)
        {
            var center = (_maxPos - _minPos) / 2f;
            Vector2 pos = Vector3.Project(eventData.position - _minPos, _maxPos - _minPos);
            var closerToLeft = pos.sqrMagnitude < (pos - (_maxPos - _minPos)).sqrMagnitude;
            if ((pos - center).sqrMagnitude > center.sqrMagnitude)
            {
                pos = closerToLeft ? Vector2.zero : _maxPos - _minPos;
            }
            Value = (pos - center).magnitude / center.magnitude * (closerToLeft ? -1 : 1);
            pos += _minPos;
            transform.position = pos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            transform.position = (_minPos + _maxPos) / 2f;
            Value = 0;
        }

        private void Awake()
        {
            _minPos = transform.TransformPoint(_minPos);
            _maxPos = transform.TransformPoint(_maxPos);
        }
    }
}