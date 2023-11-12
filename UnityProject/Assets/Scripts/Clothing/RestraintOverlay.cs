using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Logs;
using UnityEngine;
using UI.Action;
using UI.Core.Action;

namespace UI.Items
{
	/// <summary>
	/// Handles the overlays for the handcuff sprites
	/// </summary>
	public class RestraintOverlay : ClothingItem, IActionGUI
	{
		// TODO Different colored overlays for different restraints
		[SerializeField]
		private List<Sprite> handCuffOverlays = new List<Sprite>();

		[SerializeField] private SpriteRenderer spriteRend = null;
		private IEnumerator uncuffCoroutine;
		private Vector3Int positionCache;

		[SerializeField]
		private ActionData actionData = null;

		public ActionData ActionData => actionData;

		public override void SetReference(GameObject Item)
		{
			GameObjectReference = Item;
			if (Item == null)
			{
				spriteRend.sprite = null;
			}
			else
			{
				spriteRend.sprite = handCuffOverlays[referenceOffset];
			}

			if (CustomNetworkManager.IsServer)
			{
				if(thisPlayerScript == null || thisPlayerScript.Mind == null) return;
				UIActionManager.ToggleServer( gameObject ,this, GameObjectReference != null);
			}
		}

		public override void UpdateSprite()
		{
			if (GameObjectReference != null)
			{
				spriteRend.sprite = handCuffOverlays[referenceOffset];
			}
		}


		public void ServerBeginUnCuffAttempt()
		{
			if (uncuffCoroutine != null)
				StopCoroutine(uncuffCoroutine);

			float resistTime = 0;

			if (GameObjectReference == null)
			{
				Loggy.LogError($"{thisPlayerScript.playerName} cuffed but no GameObjectReference to the cuffs, so uncuffing time set to 30");

				//Default to 30 seconds
				resistTime = 30;
			}
			else
			{
				resistTime = GameObjectReference.GetComponent<Restraint>().ResistTime;
			}


			positionCache = thisPlayerScript.RegisterPlayer.LocalPositionServer;
			if (!CanUncuff()) return;

			var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Uncuff, false, false, true, false, true), TryUncuff);
			bar.ServerStartProgress(thisPlayerScript.RegisterPlayer, resistTime, thisPlayerScript.gameObject);
			Chat.AddActionMsgToChat(
				thisPlayerScript.gameObject,
				$"You are attempting to remove the cuffs. This takes up to {resistTime:0} seconds",
				thisPlayerScript.playerName + " is attempting to remove their cuffs");
		}

		private void TryUncuff()
		{
			if (CanUncuff())
			{
				thisPlayerScript.playerMove.Uncuff();
				Chat.AddActionMsgToChat(thisPlayerScript.gameObject, "You have successfully removed the cuffs",
					thisPlayerScript.playerName + " has removed their cuffs");
			}
		}

		private bool CanUncuff()
		{
			PlayerHealthV2 playerHealth = thisPlayerScript.playerHealth;

			if (playerHealth == null ||
				playerHealth.ConsciousState == ConsciousState.DEAD ||
				playerHealth.ConsciousState == ConsciousState.UNCONSCIOUS ||
				thisPlayerScript.RegisterPlayer.IsSlippingServer ||
				positionCache != thisPlayerScript.RegisterPlayer.LocalPositionServer)
			{
				return false;
			}

			return true;
		}

		public void CallActionClient()
		{
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdTryUncuff();
		}
	}
}
