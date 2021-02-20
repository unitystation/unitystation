using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AddressableReferences;
using Systems.Clothing;

/// <summary>
/// Handles the overlays for the handcuff sprites
/// </summary>
public class RestraintOverlay : ClothingItem, IActionGUI
{
	//TODO Different colored overlays for different restraints
	[SerializeField]
	private List<Sprite> handCuffOverlays = new List<Sprite>();

	[SerializeField] private SpriteRenderer spriteRend = null;

	private StandardProgressActionConfig ProgressConfig
		= new StandardProgressActionConfig(StandardProgressActionType.Escape);

	private float healthCache;
	private Vector3Int positionCache;

	[SerializeField] private AddressableAudioSource restraintRemovalSound = null;

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
		DetermineAlertUI();
	}

	public override void UpdateSprite()
	{
		if (GameObjectReference != null)
		{
			spriteRend.sprite = handCuffOverlays[referenceOffset];
		}
	}

	private void DetermineAlertUI()
	{
		if (thisPlayerScript != PlayerManager.PlayerScript) return;

		if (GameObjectReference != null)
		{
			UIActionManager.ToggleLocal(this, true);
		}
		else
		{
			UIActionManager.ToggleLocal(this, false);
		}
	}

	public void ServerBeginUnCuffAttempt()
	{
		float resistTime = GameObjectReference.GetComponent<Restraint>().ResistTime;
		healthCache = thisPlayerScript.playerHealth.OverallHealth;
		positionCache = thisPlayerScript.registerTile.LocalPositionServer;

		void ProgressFinishAction()
		{
			thisPlayerScript.playerMove.Uncuff();
			Chat.AddActionMsgToChat(thisPlayerScript.gameObject, "You have successfully removed the cuffs",
				thisPlayerScript.playerName + " has removed their cuffs");

			SoundManager.PlayNetworkedAtPos(restraintRemovalSound, thisPlayerScript.registerTile.WorldPosition, sourceObj: gameObject);
		}

		var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
			.ServerStartProgress(thisPlayerScript.gameObject.RegisterTile(), resistTime, thisPlayerScript.gameObject);
		if (bar != null)
		{
			Chat.AddActionMsgToChat(
				thisPlayerScript.gameObject,
				$"You are attempting to remove the cuffs. This will take {resistTime:0} seconds",
				thisPlayerScript.playerName + " is attempting to remove their cuffs");
		}
	}

	public void CallActionClient()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdTryUncuff();
	}
}
