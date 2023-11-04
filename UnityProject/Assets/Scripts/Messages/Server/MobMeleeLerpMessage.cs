using Systems.MobAIs;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class MobMeleeLerpMessage : ServerMessage<MobMeleeLerpMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint mob;
			public Vector2 dir;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.mob);

			if (NetworkObject == null) return;

			var getMob = NetworkObject;
			var mobMelee = getMob.GetComponent<MobMeleeAttack>();
			var mobAction = getMob.GetComponent<MobMeleeAction>();
			if (mobMelee == null && mobAction == null)
			{
				return;
			}

			if (mobMelee == null)
			{
				mobAction.ClientDoLerpAnimation(msg.dir);
				return;
			}

			mobMelee.ClientDoLerpAnimation(msg.dir);
		}

		public static NetMessage Send(GameObject mob, Vector2 dir)
		{
			//Only send to players in the area so that clients cannot snoop on mob positions
			//by watching debug log outputs from the process coroutine on this message

			NetMessage msg = new NetMessage
			{
				mob = mob.GetComponent<NetworkIdentity>().netId,
				dir = dir
			};

			SendToVisiblePlayers(mob.transform.position, msg);
			return msg;
		}
	}
}
