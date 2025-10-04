using Light2D;
using Logs;
using Mirror;
using UnityEngine;

namespace Player
{
	public class DimPlayerLightController : NetworkBehaviour
	{
		[SerializeField] private LightSprite light;
		[SyncVar] public Color lightColor = new Color(255, 255, 255, 1);
		private Color defaultColor = new Color(255, 255, 255, 1);

		private void Awake()
		{
			if (light == null)
			{
				Loggy.Error("[DimPlayerLightController] - LightSprite is null!! NREs will occur!");
				return;
			}
			defaultColor = light.Color;
		}

		public void UpdateLightLocally()
		{
			light.Color = lightColor;

			UpdateSize();
		}

		public void UpdateSize()
		{
			var Scale = light.gameObject.transform.localScale;
			Scale = Vector3.one *4;

			if (light.Color.a == 0)
			{
				Scale = Vector3.zero;
			}
			else
			{
				Scale = (light.Color.a / 0.0392156877f) * Scale;
			}

			light.gameObject.transform.localScale = Scale;
		}

		public void TurnOffLight2D()
		{
			light.SetActive(false);
		}

		public void TurnOnLight2D()
		{
			light.SetActive(true);
			UpdateLightLocally();
		}

		public void ResetToDefault()
		{
			lightColor = defaultColor;
			UpdateSize();
		}
	}
}