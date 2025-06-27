using System;
using System.Linq;
using Actions.V2;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2.Living.Mutations.Surface
{
	public class BodySpritesInvisbility : NetworkBehaviour
	{
		[SyncVar(hook = nameof(OnAlphaChanged))] public float Alpha = 1f;
		public GameObject bodyPartSprites;
		public GameObject Customisation;

		public bool DEBUG = false;

		private void Start()
		{
			if (DEBUG == false) return;
			_ = SetupDebugActions();
		}

		private async UniTask SetupDebugActions()
		{
			var playerScript = GetComponent<PlayerScript>();
			playerScript.PlayerButtonedActions.RegisterNewAction($"{gameObject.NetId()}_player_become_invisible", "Become Invisible",
				"Make yourself invisible to other players.", ActionTriggerType.ServerOnly, null, () =>
				{
					if (!gameObject)
					{
						Loggy.Error($"Gameobject is null?");
						return;
					}
					Alpha = 0.1f;
					Chat.AddExamineMsg(gameObject, "You have become invisible to other players.");
				}, cooldownTime: 8f);
			while (playerScript.PlayerButtonedMindActions == null)
			{
				await UniTask.WaitForSeconds(2f);
			}
			playerScript.PlayerButtonedMindActions?.RegisterNewAction($"{gameObject.NetId()}_player_become_visible", "Become visible",
				"Make yourself visible to other players.", ActionTriggerType.ServerOnly, null, () =>
				{
					if (!gameObject)
					{
						Loggy.Error($"Gameobject is null?");
						return;
					}
					Alpha = 1f;
					Chat.AddExamineMsg(gameObject, "You have become visible to other players.");
				}, cooldownTime: 8f);
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
			foreach (SpriteRenderer spriteRenderer in bodyPartSprites.GetComponentsInChildren<SpriteRenderer>())
			{
				spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
			}
			if (Customisation == null)return;
			foreach (SpriteRenderer spriteRenderer in Customisation.GetComponentsInChildren<SpriteRenderer>())
			{
				if (spriteRenderer.GetComponent<CustomisationSprite>() == null) continue;

				spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
			}
		}
	}
}