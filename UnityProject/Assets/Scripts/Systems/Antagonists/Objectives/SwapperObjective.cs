using System.Linq;
using Antagonists;
using Systems.Spells.Swapper;
using UnityEngine;

namespace Antagonists
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