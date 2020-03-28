using System.Collections;
using Mirror;
using UnityEngine;

public class MobMeleeLerpMessage : ServerMessage
{
	public override short MessageType => (short) MessageTypes.MobMeleeLerpMessage;

	public uint mob;
	public Vector2 dir;

	public override IEnumerator Process()
	{
		yield return null;
		if (mob == NetId.Empty) yield break;

		var getMob = NetworkIdentity.spawned[mob];
		var mobMelee = getMob.GetComponent<MobMeleeAttack>();
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
