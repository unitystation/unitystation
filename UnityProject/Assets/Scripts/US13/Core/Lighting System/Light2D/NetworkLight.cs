using System;
using Light2D;
using UnityEngine;
using Mirror;

namespace Core.Lighting_System.Light2D
{
	public class NetworkLight: NetworkBehaviour
	{
		private Color baseColour;
		public Color BaseColour => baseColour;

		private LightSprite lightSprite;

		[SyncVar(hook = nameof(SyncColour))]
		private Color currentColour;

		private void Awake()
		{
			lightSprite = GetComponent<LightSprite>();
			baseColour = lightSprite.Color;

			//Used to make mirror cache network identity so it wont do get component checks in atmos thread
			var netIdSet = netId;
		}

		[Server]
		public void SetColour(Color newColour)
		{
			currentColour = newColour;
		}

		private void SyncColour(Color oldColour, Color newColour)
		{
			lightSprite.Color = newColour;
		}
	}
}
