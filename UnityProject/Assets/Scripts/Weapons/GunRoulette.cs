using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;

public class GunRoulette : Gun
{
	[HideInInspector]
	public int untestedChambers = 6;
	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (isSound) return false; //prevent player spamming this action

		isSound = true;
		Chat.AddActionMsgToChat(interaction.Performer, "You spin the cylinder of the revolver", $"{interaction.Performer.ExpensiveName()} spins the cylinder of the revolver ");
		SoundManager.PlayNetworkedAtPos("RevolverSpin", transform.position, sourceObj: serverHolder);
		WaitFor.Seconds(0.2f);
		untestedChambers = 6;
		isSound = false;
	}

	private void PlayEmptySFX()
	{
		SoundManager.PlayNetworkedAtPos("EmptyGunClick", transform.position, sourceObj: serverHolder);
	}

	public override void ServerPerformInteraction(AimApply interaction)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (CurrentMagazine == null)
		{
			PlayEmptySFX();
			if (interaction.Performer != PlayerManager.LocalPlayer)
			{
				Logger.LogTrace("Server rejected shot - No magazine being loaded", Category.Firearms);
			}
			return;
		}
		else
		{
			Chat.AddActionMsgToChat(interaction.Performer, "You point the revolver at your head, pulling the trigger", $"{interaction.Performer.ExpensiveName()} points the revolver at their head and pulls the trigger!");
			int firedChamber = Random.Range(1,untestedChambers);
			if (firedChamber == 1 && untestedChambers != 0)
			{
				untestedChambers = 6;
				Chat.AddActionMsgToChat(interaction.Performer, "You shoot yourself point blank in the head!", $"{interaction.Performer.ExpensiveName()} shoots themself point blank in the head!");
				//enqueue the shot (will be processed in Update)
				ServerShoot(interaction.Performer, interaction.TargetVector.normalized, BodyPartType.Head, true);
			}
			else
			{
				untestedChambers--;
				PlayEmptySFX();
				Chat.AddActionMsgToChat(interaction.Performer, "The revolver makes a clicking sound", "The revolver makes a clicking sound");
				return;
			}
		}
	}
}