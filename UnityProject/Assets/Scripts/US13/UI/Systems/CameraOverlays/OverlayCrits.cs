using UnityEngine;
using US13.Player;
using Util;

namespace US13.UI.Systems.CameraOverlays
{
	public class OverlayCrits : MonoBehaviour
	{
		public static OverlayCrits Instance;

		public Shader shader;
		private Material critMaterial;

		private bool MonitorTarget = false;

		private float inHealth = 100.0f;
		private float outHealth = 100.0f;

		public void Awake()
		{
			Instance = this;
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (shader == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			if (critMaterial == false) critMaterial = new Material(shader);

			if (PlayerManager.LocalPlayerScript.OrNull()?.Mind != null && PlayerManager.LocalMindScript.IsGhosting || PlayerManager.LocalPlayerScript?.playerHealth == null)
			{
				inHealth = 100.0f; //Clear crit effect but gradually
			}

			float t = 1f - Mathf.Exp(-5f * Time.deltaTime);
			outHealth = Mathf.Lerp(outHealth, inHealth, t);


			critMaterial.SetFloat("_CurrentHealth", outHealth);
			Graphics.Blit(source, destination, critMaterial);
		}

		public void SetNewHealthValue(float newHealth)
		{
			inHealth = newHealth;
		}
	}
}