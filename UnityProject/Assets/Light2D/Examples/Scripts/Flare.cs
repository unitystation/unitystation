using UnityEngine;

namespace Light2D.Examples
{
    public class Flare : MonoBehaviour
    {
        private float _lifetimeElapsed;
        private Color _startColor;
        public float AlphaGrowTime = 0.5f;
        public float Lifetime;
        public LightSprite Light;

        private void Start()
        {
            _startColor = Light.Color;
            Light.Color = _startColor.WithAlpha(0);
        }

        private void Update()
        {
            _lifetimeElapsed += Time.deltaTime;


            if (_lifetimeElapsed > Lifetime)
            {
                _lifetimeElapsed = Lifetime;
                Destroy(gameObject);
            }


            var alpha = Mathf.Lerp(0, _startColor.a, Mathf.Min(_lifetimeElapsed, AlphaGrowTime) / AlphaGrowTime);
            Light.Color = Color.Lerp(_startColor.WithAlpha(alpha), _startColor.WithAlpha(0),
                _lifetimeElapsed / Lifetime);
        }
    }
}