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
