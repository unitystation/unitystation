using Light2D;
using Logs;
using Mirror;
using UnityEngine;
using Util;

namespace US13.Player
{
	public class DimPlayerLightController : NetworkBehaviour
	{
		[SerializeField] private LightSprite light;
		[SyncVar(hook = nameof(SynclightColor))] public Color lightColor = new Color(255, 255, 255, 10);
		private Color _defaultColor = new Color(255, 255, 255, 10);

		public const float DEFAULT_SIZE = 4;
		private const float ALPHA_SCALE_FACTOR = 25; //Scales the light sprite by the light colours alpha
		private float _size = DEFAULT_SIZE; //Override for dim light size independent from the light sprite colour

		private void Awake()
		{
			if (light == null)
			{
				Loggy.Error("[DimPlayerLightController] - LightSprite is null!! NREs will occur!");
				return;
			}

			_defaultColor = lightColor;
		}

		public void UpdateLightLocally()
		{
			UpdateLightData(_size);
		}


		public void SynclightColor(Color oldc, Color newc)
		{
			lightColor = newc;
			UpdateLightLocally();
		}


		public void UpdateLightData(float newSize, bool updateColour = true)
		{
			if(updateColour) light.Color = lightColor;

			_size = newSize;
			var scale = Vector3.one * _size;

			scale = light.Color.a == 0 ? Vector3.zero : light.Color.a * ALPHA_SCALE_FACTOR * scale;
			light.gameObject.transform.localScale = scale;
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
			lightColor = _defaultColor;
			UpdateLightData(DEFAULT_SIZE);
		}
	}
}