using UnityEngine;

namespace Light2D.Examples
{
    public class SimpleExamplesController : MonoBehaviour
    {
        private int _currColorIndex;
        private int _currExampleIndex;
        public LightSprite[] ColoredLights = new LightSprite[0];
        public GameObject[] Examples = new GameObject[0];
        public Color[] LightColors = {Color.white};

        private void Start()
        {
            UpdateExample();
            UpdateColors();
        }

        private void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                _currExampleIndex++;
                if (_currExampleIndex >= Examples.Length)
                {
                    _currExampleIndex = 0;
                }

                UpdateExample();
            }

            if (Input.GetMouseButtonUp(1))
            {
                _currColorIndex++;
                if (_currColorIndex >= LightColors.Length)
                {
                    _currColorIndex = 0;
                }

                UpdateColors();
            }
        }

        private void UpdateColors()
        {
            var color = LightColors.Length == 0 ? Color.white : LightColors[_currColorIndex];
            for (var i = 0; i < ColoredLights.Length; i++)
            {
                ColoredLights[i].Color = color.WithAlpha(ColoredLights[i].Color.a);
            }
        }

        private void UpdateExample()
        {
            for (var i = 0; i < Examples.Length; i++)
            {
                Examples[i].SetActive(i == _currExampleIndex);
            }
            LightingSystem.Instance.LoopAmbientLight(20);
        }
    }
}