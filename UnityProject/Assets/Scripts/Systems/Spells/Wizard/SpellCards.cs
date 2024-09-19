using System.Collections;
using System.Collections.Generic;
using Systems.Spells;
using UnityEngine;

namespace Systems.Spells.Wizard
{
	public class SpellCards : ProjectileSpell
	{
		public override void UpgradeTier()
		{
			ProjectilesPerUse += 2;
			base.UpgradeTier();
		}
	}
}
