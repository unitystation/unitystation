
using UnityEngine;
using US13.Core.Camera;
using US13.Player;
using Util;

namespace US13.UI.Systems.CameraOverlays
{
    public class OverlayCrits : CameraEffectBase
    {
        public static OverlayCrits Instance;
        private static readonly int currentHealth = Shader.PropertyToID("_CurrentHealth");

        [SerializeField]
        private Material material;

        [SerializeField]
        private Shader shader;

        [SerializeField]
        private float healthLerpSpeed = 5f;

        private Material runtimeMaterial;
        private float inHealth = 100f;
        private float outHealth = 100f;

        private void Awake()
        {
            Instance = this;
            EnsureMaterial();
        }

        private void EnsureMaterial()
        {
            if (material != null || runtimeMaterial != null) return;

            if (shader == null) shader = Shader.Find("Hidden/Crit State");

            if (shader == null)
            {
                Debug.LogError(
                    "OverlayCrits: Could not find shader 'Hidden/Crit State'. " +
                    "Make sure the shader is included in the build.",
                    this
                );
                return;
            }

            runtimeMaterial = new Material(shader)
            {
                name = "Runtime Crit State Material",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        public override Material GetMaterial()
        {
            EnsureMaterial();

            Material mat = material != null ? material : runtimeMaterial;
            if (mat == null) return null;

            mat = GetInstance(mat);
            if (mat == null) return null;

            mat.SetFloat(currentHealth, outHealth);
            return mat;
        }

        private void Update()
        {
            var localPlayer = PlayerManager.LocalPlayerScript.OrNull();

            if ((localPlayer?.Mind != null && PlayerManager.LocalMindScript.IsGhosting) || localPlayer?.playerHealth == null)
            {
                inHealth = 100f;
            }

            float t = 1f - Mathf.Exp(-healthLerpSpeed * Time.deltaTime);
            outHealth = Mathf.Lerp(outHealth, inHealth, t);
        }

        public void SetNewHealthValue(float newHealth)
        {
            inHealth = newHealth;
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (runtimeMaterial == null) return;
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }
}
