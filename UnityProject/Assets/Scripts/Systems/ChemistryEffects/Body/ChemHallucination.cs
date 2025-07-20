using System.Collections;
using System.Collections.Generic;
using Core.Chat;
using HealthV2;
using ScriptableObjects.RP;
using UnityEngine;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/Hallucination")]
	public class ChemHallucination : Chemistry.Effect
	{
		[Tooltip("Adds hallucination time")]
		[SerializeField] private float hallucinationTime = 1;

		[Tooltip("Chance for this to take effect")]
		[SerializeField] private float percentageChance = 100;


		[Tooltip("Chance for the victim to suddenly switch to harm intent")]
		[SerializeField] private float harmIntentChance = 50;

		[Tooltip("Chance for the victim to suddenly forget/remember names")]
		[SerializeField] private float nameForgetChance = 25;

		[SerializeField] private List<string> ParanoidThoughts = new List<string>();

		[Tooltip("Chance for the victim to perform the following emote")]
		[SerializeField] private float emoteChance = 25;
		[SerializeField] protected List<EmoteSO> possibleEmotes;


		public override void Apply(MonoBehaviour sender, float amount)
		{
			if (Random.Range(0, 100)>percentageChance)
			{
				PlayerScript player = sender.GetComponent<PlayerScript>();
				if (player is null) return;

				if(DMMath.Prob(harmIntentChance)) player.PlayerNetworkActions.CmdSetCurrentIntent(Intent.Harm);
				if (DMMath.Prob(nameForgetChance)) player.playerHealth.CannotRecognizeNames = !player.playerHealth.CannotRecognizeNames;
				if (DMMath.Prob(emoteChance)) EmoteActionManager.DoEmote(possibleEmotes.PickRandom(), player.gameObject);
			}
		}
	}
}
