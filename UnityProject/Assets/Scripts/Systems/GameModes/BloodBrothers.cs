using Antagonists;
using Systems.Antagonists.Antags;
using Systems.Explosions;
using UnityEngine;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/BloodBrothers")]
	public class BloodBrothers : GameMode
	{
		public static void OnBrotherDeath()
		{
			foreach (var possibleBrother in AntagManager.Instance.ActiveAntags)
			{
				if (possibleBrother.Antagonist is not BloodBrother) continue;
				Chat.AddExamineMsg(possibleBrother.Owner.Body.gameObject,
					"You feel your fellow brother part ways with their body.. And you follow them.");
				possibleBrother.Owner.CurrentPlayScript.playerHealth.Death();
				if (DMMath.Prob(15))
				{
					Explosion.StartExplosion(possibleBrother.Owner.Body.gameObject.AssumedWorldPosServer().CutToInt(), 750);
				}
			}
		}
	}
}