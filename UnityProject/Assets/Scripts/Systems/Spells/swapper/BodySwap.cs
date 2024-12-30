using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Logs;
using UnityEngine;
using Weapons.Projectiles;

namespace Systems.Spells.Swapper
{
	public class BodySwap : Spell
	{
		public List<LivingHealthMasterBase> PreviouslyJumpedTo = new List<LivingHealthMasterBase>();

		public bool PreventJumpingBack = true;

		public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition)
		{
			Vector3Int casterWorldPos = caster.Script.WorldPos;
			if ((casterWorldPos - clickPosition).magnitude > 2)
			{
				Chat.AddExamineMsg(caster.GameObject, "Target is too far away");
				return false;
			}

			var creatures = MatrixManager.GetAt<LivingHealthMasterBase>(clickPosition.RoundToInt(), true);
			if (creatures.Any() == false)
			{
				Chat.AddExamineMsg(caster.GameObject, "Nothing to swap with");
				return false;
			}

			var Being = creatures.First();

			Loggy.Error("Being > " + Being.name);

			if (Being == caster.Script.GetComponent<LivingHealthMasterBase>())
			{
				Chat.AddExamineMsg(caster.GameObject, "you can't swap with yourself");
				return false;
			}


			if (PreventJumpingBack)
			{
				if (PreviouslyJumpedTo.Contains(Being)) //TODO Maybe make it mind based idk
				{
					Chat.AddExamineMsg(caster.GameObject, "You have already swapped with the target");
					return false;
				}
			}

			if (PreviouslyJumpedTo.Any() == false)
			{
				PreviouslyJumpedTo.Add(caster.Script.GetComponent<LivingHealthMasterBase>());
			}

			PreviouslyJumpedTo.Add(Being);

			if (Being?.brain?.PossessingMind == null)
			{
				Chat.AddExamineMsg(caster.GameObject, "The target does not have a mind");
				return false;
			}

			var casterMind = caster.Mind;

			Being.brain.PossessingMind.SetPossessingObject( caster.Script.GameObject);

			casterMind.SetPossessingObject(Being.gameObject);

			return true;
		}
	}
}