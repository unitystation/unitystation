using UnityEngine;

namespace Light2D.Examples
{
    [RequireComponent(typeof(LightSprite))]
    public class LightRandomizer : MonoBehaviour
    {
        public float MaxLightAlpha = 0.8f;
        public float MaxRadius = 35;
        public float MinLightAlpha = 0.3f;
        public float MinRadius = 5;

        private void Start()
        {
            var rend = GetComponent<LightSprite>();

            rend.transform.localScale = Vector3.one * Random.Range(MinRadius, MaxRadius);
            var c = new Vector4(Random.value, Random.value, Random.value, Random.Range(MinLightAlpha, MaxLightAlpha));
            var maxc = Mathf.Max(c.x, c.y, c.z);
            rend.Color = new Color(c.x / maxc, c.y / maxc, c.z / maxc, c.w);
        }
    }
}