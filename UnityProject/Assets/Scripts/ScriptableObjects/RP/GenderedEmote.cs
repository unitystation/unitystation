using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;
using Messages.Server.SoundMessages;

[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/GenderedEmote")]
public class GenderedEmote : EmoteSO
{
	[SerializeField]
	private string critViewText = "screams in pain!";

	private string viewText_Final;

	
	public override void Do(GameObject player)
	{
		BodyType playerGender = checkPlayerGender(player);
		PlayerHealthV2 playerHealth = getPlayerHealth(player);
		checkPlayerState(playerHealth, playerGender);
		Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewText_Final}.");
		playAudio(audioToUse, player);
	}

	private void checkPlayerState(PlayerHealthV2 health, BodyType gender)
	{
		HealthCheck(health);
		genderCheck(gender);
	}

	private void HealthCheck(PlayerHealthV2 health)
	{
		if (health.IsDead)
		{
			return;
		}
		if (health.IsCrit)
		{
			viewText_Final = critViewText;
		}
		else
		{
			viewText_Final = viewText;
		}
	}
}
