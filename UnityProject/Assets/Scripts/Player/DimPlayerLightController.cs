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
		private PlayerScript player;

		private void Awake()
		{
			if (light == null)
			{
				Loggy.LogError("[DimPlayerLightController] - LightSprite is null!! NREs will occur!");
				return;
			}
			defaultColor = light.Color;
			player = GetComponent<PlayerScript>();
		}

		public void UpdateColor(Color oldValue, Color newValue)
		{
			if (oldValue == newValue) return;
			lightColor = newValue;
			RpcSetForPlayerOnly(player.connectionToClient, newValue);
		}

		[TargetRpc]
		public void RpcSetForPlayerOnly(NetworkConnection connection, Color color)
		{
			light.Color = color;
		}

		public void UpdateLightLocally()
		{
			light.Color = lightColor;
		}

		public void ResetToDefault()
		{
			lightColor = defaultColor;
		}
	}
}