using HealthV2;
using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Suicide", menuName = "ScriptableObjects/RP/Emotes/Suicide")]
	public class Suicide : EmoteSO
	{
		public override void Do(GameObject actor)
		{
			var playerScript = actor.GetComponent<PlayerScript>();
			if (playerScript.DynamicItemStorage == null) return;

			//Just end the misery early if the player has been stuck in suicide for a while now.
			if (CheckPlayerCritState(actor))
			{
				actor.GetComponent<LivingHealthMasterBase>()?.Death();
				return;
			}

			var activeHandSlot = playerScript.DynamicItemStorage.OrNull()?.GetActiveHandSlot();
			if (activeHandSlot == null || activeHandSlot.IsEmpty) return; //Assuming we have no hand or no item in hand
			if (activeHandSlot.ItemObject.TryGetComponent<ISuicide>(out var suicideObject) == false)
			{
				Chat.AddExamineMsg(actor, "You do not have a lethal object to commit suicide with.");
				return;
			}
			if (suicideObject.CanSuicide(actor) == false) return;
			playerScript.StartCoroutine(suicideObject.OnSuicide(actor));
		}
	}
}