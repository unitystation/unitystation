using Logs;
using UnityEngine;
using US13.HealthV2;
using US13.Managers;
using US13.Projectiles;
using US13.Systems.Explosions;
using US13.Systems.Explosions.NodeTypes;
using US13.Systems.Spells;
using Util;

public class DisableTechnology : Spell
{


	public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition, BodyPartType targetZone)
	{
		Vector3Int casterWorldPos = caster.Script.gameObject.AssumedWorldPosServer().RoundToInt();
		Explosion.StartExplosion(casterWorldPos, 1000f, new ExplosionEmpNode(casterWorldPos));
		return true;
	}
}
