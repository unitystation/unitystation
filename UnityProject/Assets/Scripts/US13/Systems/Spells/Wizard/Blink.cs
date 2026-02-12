using US13.Managers;
using US13.Player;

namespace US13.Systems.Spells.Wizard
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
