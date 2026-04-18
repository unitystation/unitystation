using UnityEngine;
using US13.HealthV2.Living.MedicalChemistry;
using US13.Player;
using US13.Systems.Ai;

namespace US13.Systems.Antagonists.Antags
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Vampire")]
	public class Vampire : Antagonist
	{
		public override void AfterSpawn(Mind NewMind) { }
	}
}
