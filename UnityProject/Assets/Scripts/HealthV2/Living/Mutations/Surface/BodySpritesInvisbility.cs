using System;
using Logs;
using Mirror;
using UnityEngine;

namespace HealthV2.Living.Mutations.Surface
{
	public class BodySpritesInvisbility : NetworkBehaviour
	{
		[SyncVar(hook = nameof(OnAlphaChanged))] public float Alpha = 1f;
		public GameObject bodyPartSprites;

		public void OnAlphaChanged(float oldAlpha, float newAlpha)
		{
			if (newAlpha < 0.05f)
			{
				newAlpha = 0.05f;
			}
			if (newAlpha > 1f)
			{
				newAlpha = 1f;
			}
			if (bodyPartSprites == null) return;
			Loggy.Info($"setting alpha to {newAlpha}");
			foreach (SpriteRenderer spriteRenderer in bodyPartSprites.GetComponentsInChildren<SpriteRenderer>())
			{
				spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
			}
		}
	}
}