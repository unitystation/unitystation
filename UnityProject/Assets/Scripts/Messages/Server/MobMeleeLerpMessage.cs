using System.Collections;
using Mirror;
using NPC.AI;
using UnityEngine;

public class MobMeleeLerpMessage : ServerMessage
{
	public uint mob;
	public Vector2 dir;

	public override void Process()
	{
		if (mob == NetId.Empty) return;

		var getMob = NetworkIdentity.spawned[mob];
		var mobMelee = getMob.GetComponent<MobMeleeAttack>();
		if (mobMelee == null)
		{
			var mobAction = getMob.GetComponent<MobMeleeAction>();
			mobAction.ClientDoLerpAnimation(dir);
			return;
		}
		mobMelee.ClientDoLerpAnimation(dir);
	}

	public static MobMeleeLerpMessage Send(GameObject mob, Vector2 dir)
	{
		//Only send to players in the area so that clients cannot snoop on mob positions
		//by watching debug log outputs from the process coroutine on this message

		MobMeleeLerpMessage msg = new MobMeleeLerpMessage
		{
			mob = mob.GetComponent<NetworkIdentity>().netId,
			dir = dir
		};

		msg.SendToVisiblePlayers(mob.transform.position);

		return msg;
	}
}
