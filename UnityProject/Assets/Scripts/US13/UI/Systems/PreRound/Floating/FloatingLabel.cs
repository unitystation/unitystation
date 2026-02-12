using TMPro;
using UnityEngine;
using Util;

namespace US13.UI.Systems.PreRound.Floating
{
    public sealed class FloatingLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _uiLabel;
        private float _showDotThreshold = 0.65f;
        private float _mouseProximityThreshold = 320f; // pixels
        private Camera cam;

        private void Awake()
        {
            if (_uiLabel == null)
                _uiLabel = GetComponentInChildren<TMP_Text>();

            cam = Camera.main;
        }

        private void Update()
        {
            UpdateVisibility();
        }

        public void UpdateVisibility()
        {
            if (!cam) return;
            Vector2 mousePos = Input.mousePosition;
            Vector2 elementScreenPos = RectTransformUtility.WorldToScreenPoint(null, transform.position);
            float distance = Vector2.Distance(elementScreenPos, mousePos);
			_uiLabel.SetActive(distance <= _mouseProximityThreshold);
        }
    }
}
