using UnityEngine;
using US13.HealthV2.Living.MedicalChemistry;
using US13.Player;
using US13.Systems.Ai;

namespace US13.Systems.Antagonists.Antags
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Vampire")]
	public class Vampire : Antagonist
	{
		public override void AfterSpawn(Mind NewMind)
		{
			if (NewMind.GetCurrentMob().TryGetComponent<AiPlayer>(out var aiPlayer)) return;

			NewMind.Body.playerHealth.reagentPoolSystem.BloodPool.Add(CommonSicknesses.Instance.VampirismReagent, 5);
			//Game start vampires should start with ability to blood drain
		}

	}
}
