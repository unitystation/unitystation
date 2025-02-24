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

		private void Start()
		{
			OnAlphaChanged(1f, Alpha);
		}

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
			foreach (SpriteRenderer renderer in bodyPartSprites.GetComponentsInChildren<SpriteRenderer>())
			{
				renderer.color = new Color(1f, 1f, 1f, newAlpha);
			}
		}
	}
}