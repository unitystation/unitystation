using System.Linq;
using UnityEngine;
using US13.Systems.Spells.swapper;

namespace US13.Systems.Antagonists.Objectives
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/SwapperObjective")]
	public class SwapperObjective : Objective
	{
		protected override void Setup()
		{
		}

		protected override bool CheckCompletion()
		{

			var Spell =  this.Owner.Spells.FirstOrDefault(x => x is BodySwap) as BodySwap ;

			if (Spell == null)
			{
				return false;
			}

			if (Spell.PreviouslyJumpedTo.Count == 0)
			{
				return false;
			}



			var JumpedNumber = (Spell.PreviouslyJumpedTo.Count - 1); //-1 Because it also includes the original body That should be excluded

			description += "Number body swaped " + JumpedNumber;

			return true;

		}
	}
}