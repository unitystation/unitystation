using UnityEngine;

namespace Light2D.Examples
{
    public class ColorTweener : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Color _startColor;
        private Color _targetColor;
        private float _timer;
        public float ColorMul = 2;
        public float TweenInterval = 2.5f;

        private void OnEnable()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            _timer -= Time.deltaTime;

            if (_timer <= 0)
            {
                _timer = TweenInterval;
                _startColor = _spriteRenderer.color;
                _targetColor = new Vector4(
                    Mathf.Clamp01(Random.value * ColorMul), Mathf.Clamp01(Random.value * ColorMul),
                    Mathf.Clamp01(Random.value * ColorMul), Mathf.Clamp01(Random.value * ColorMul));
            }

            _spriteRenderer.color = Color.Lerp(_startColor, _targetColor, 1 - _timer / TweenInterval);
        }
    }
}