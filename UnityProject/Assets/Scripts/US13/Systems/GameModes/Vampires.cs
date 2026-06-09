using Logs;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.HealthV2.Living.MedicalChemistry;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.Systems.Antagonists;

namespace US13.Systems.GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Vampires")]
	public class Vampires : GameMode
	{
		protected override void SpawnAntag(PlayerSpawnRequest playerSpawnRequest, Antagonist Antagonist)
		{
			var antag = Antagonist;
			if (!AllocateJobsToAntags && antag.AntagOccupation == null)
			{
				Loggy.Error().Format("AllocateJobsToAntags is false but {0} AntagOccupation is null! " +
				                     "Game mode must either set AllocateJobsToAntags or possible antags neeed an AntagOccupation.",
					Category.Antags, antag.AntagName);
				return;
			}

			ReagentPoolSystem poolSystem = playerSpawnRequest.Player?.Script?.playerHealth?.reagentPoolSystem;
			if (poolSystem == null)
			{
				Loggy.Error().Format("Failed to find reagentPoolSystem on spawned Vampire", Category.Antags);
				return;
			}
			poolSystem.BloodPool.Add(CommonSicknesses.Instance.VampirismReagent, 5.0f); //
		}
	}
}
