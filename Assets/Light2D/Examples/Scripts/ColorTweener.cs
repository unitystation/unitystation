using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Light2D.Examples
{
    public class ColorTweener : MonoBehaviour
    {
        public float TweenInterval = 2.5f;
        public float ColorMul = 2;
        private SpriteRenderer _spriteRenderer;
        private float _timer;
        private Color _targetColor;
        private Color _startColor;

        void OnEnable()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (_spriteRenderer == null)
                return;

            _timer -= Time.deltaTime;

            if (_timer <= 0)
            {
                _timer = TweenInterval;
                _startColor = _spriteRenderer.color;
                _targetColor = new Vector4(
                    Mathf.Clamp01(Random.value*ColorMul), Mathf.Clamp01(Random.value*ColorMul),
                    Mathf.Clamp01(Random.value*ColorMul), Mathf.Clamp01(Random.value*ColorMul));
            }

            _spriteRenderer.color = Color.Lerp(_startColor, _targetColor, 1 - _timer/TweenInterval);
        }
    }
}
