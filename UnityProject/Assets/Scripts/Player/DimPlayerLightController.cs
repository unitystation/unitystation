using System;
using Light2D;
using Logs;
using Mirror;
using UnityEngine;

namespace Player
{
	public class DimPlayerLightController : NetworkBehaviour
	{
		[SerializeField] private LightSprite light;
		[SyncVar(hook = nameof(UpdateColor))] public Color lightColor = new Color(255, 255, 255, 1);
		private Color defaultColor = new Color(255, 255, 255, 1);

		private void Awake()
		{
			if (light == null)
			{
				Loggy.LogError("[DimPlayerLightController] - LightSprite is null!! NREs will occur!");
				return;
			}
			defaultColor = light.Color;
		}

		public void UpdateColor(Color oldValue, Color newValue)
		{
			if (oldValue == newValue) return;
			lightColor = newValue;
			light.Color = newValue;
		}

		public void ResetToDefault()
		{
			lightColor = defaultColor;
		}
	}
}