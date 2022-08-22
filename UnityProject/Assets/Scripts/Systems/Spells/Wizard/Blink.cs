using UnityEngine;
using System.Collections;
using Systems.Teleport;

namespace Systems.Spells.Wizard
{
	public class Blink : Spell
	{
		public override bool CastSpellServer(PlayerInfo caster)
		{
			TeleportUtils.ServerTeleportRandom(caster.GameObject, 8, 16, true, true);

			return true;
		}
	}
}
