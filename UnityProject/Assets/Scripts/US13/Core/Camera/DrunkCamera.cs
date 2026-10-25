
using UnityEngine;

namespace US13.Core.Camera
{
    public class DrunkCamera : CameraEffectBase
    {
        public Material material;

        [Range(0f, 10f)]
        public float LightScale = 5f;

        [Range(0f, 0.1f)]
        public float VertWave = 0.05f;

        [Range(0f, 0.1f)]
        public float HozWave = 0.05f;

        [Range(0f, 0.5f)]
        public float Waves = 0.25f;

        [Range(0f, 1f)]
        public float Speed = 0.5f;

        [Range(0f, 0.02f)]
        public float DoubleVision = 0.01f;

        public void SetDrunkStrength(float strength)
        {
            strength = Mathf.Clamp01(strength);

            VertWave = Mathf.Lerp(0f, 0.06f, strength);
            HozWave = Mathf.Lerp(0f, 0.07f, strength);
            Waves = Mathf.Lerp(0f, 0.5f, strength);
            Speed = Mathf.Lerp(0f, 0.6f, strength);
            DoubleVision = Mathf.Lerp(0f, 0.015f, strength);

            enabled = true;
        }

        public override Material GetMaterial()
        {
            Material mat = GetInstance(material);
            if (mat == null)
                return null;

            mat.SetFloat("_LightAmount", LightScale);
            mat.SetFloat("_CosAmount", VertWave);
            mat.SetFloat("_SinAmount", HozWave);
            mat.SetFloat("_Waves", Waves);
            mat.SetFloat("_Speed", Speed);
            mat.SetFloat("_DoubleVision", DoubleVision);

            return mat;
        }
    }
}
