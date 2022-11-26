using HealthV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Chat;
using ScriptableObjects.RP;

namespace Systems.Research
{
	/// <summary>
	/// Simulates the effects of the paranoia sickness but localised to those in its proximity.
	/// </summary>
	[CreateAssetMenu(fileName = "ParanoiaAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/ParanoiaAreaEffect")]
	public class ParanoiaAreaEffect : AreaEffectBase
	{
		[SerializeField] private List<string> ParanoidThoughts = new List<string>();

		[SerializeField] private int HarmIntentChance = 50;

		[SerializeField] private int NameForgetChance = 50;

		[SerializeField] private int EmoteChance = 50;
	
		[SerializeField] protected EmoteSO emoteFeedback;

		public override void OnEffect(PlayerScript player, BodyPart part = null)
		{
			Chat.AddExamineMsg(player.gameObject, ParanoidThoughts.PickRandom());

			if(DMMath.Prob(HarmIntentChance))
			{
				player.PlayerNetworkActions.CmdSetCurrentIntent(Intent.Harm);
			}
			if (DMMath.Prob(NameForgetChance))
			{
				player.playerHealth.CannotRecognizeNames = !player.playerHealth.CannotRecognizeNames;
			}
			if (DMMath.Prob(HarmIntentChance))
			{
				EmoteActionManager.DoEmote(emoteFeedback, player.gameObject);
			}
		}
	}
}
