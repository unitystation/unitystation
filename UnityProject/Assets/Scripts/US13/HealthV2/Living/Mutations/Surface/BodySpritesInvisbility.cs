using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using UnityEngine;
using US13.Actions.V2;
using US13.Core.Chat;
using US13.Player;
using Util;

namespace US13.HealthV2.Living.Mutations.Surface
{
	public class BodySpritesInvisbility : NetworkBehaviour
	{

		[SyncVar(hook = nameof(OnIncludeClothesChanged))]
		public bool IncludeClothes = false;
		[SyncVar(hook = nameof(OnAlphaChanged))] public float Alpha = 1f;
		public GameObject bodyPartSprites;
		public GameObject Customisation;
		public GameObject Huds;


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
				"Make yourself invisible to other players.", ActionTriggerType.ServerOnly, null, v =>
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
				"Make yourself visible to other players.", ActionTriggerType.ServerOnly, null, v =>
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

		private void SetAlphaOnSprites(float newAlpha)
		{
			if (bodyPartSprites != null)
			{
				foreach (SpriteRenderer spriteRenderer in bodyPartSprites.GetComponentsInChildren<SpriteRenderer>())
				{
					spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
				}
			}

			if (Customisation != null)
			{
				foreach (SpriteRenderer spriteRenderer in Customisation.GetComponentsInChildren<SpriteRenderer>())
				{
					if (spriteRenderer.GetComponent<CustomisationSprite>() == null)
					{
						if (IncludeClothes == false)
						{
							if (newAlpha != 1)
							{
								continue;
							}
						}
					}

					spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
				}
			}

			if (Huds != null)
			{
				foreach (SpriteRenderer spriteRenderer in Huds.GetComponentsInChildren<SpriteRenderer>())
				{
					spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
				}
			}
		}

		public void OnIncludeClothesChanged(bool oldClothes, bool newClothes)
		{
			SetAlphaOnSprites(Alpha);
		}

		public void OnAlphaChanged(float oldAlpha, float newAlpha)
		{
			SetAlphaOnSprites(newAlpha);
		}

		[ClientRpc]
		public void ServerToClientsResetAlphaToOne()
		{
			Alpha = 1f;
			SetAlphaOnSprites(0.99f);
		}
	}
}