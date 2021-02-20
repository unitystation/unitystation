using System.Collections;
using Mirror;
using UnityEngine;

namespace Systems.MobAIs
{
	public class MobMeleeLerpMessage : ServerMessage
	{
		public class MobMeleeLerpMessageNetMessage : ActualMessage
		{
			public uint mob;
			public Vector2 dir;
		}

		public override void Process(ActualMessage msg)
		{
			var newMsg = msg as MobMeleeLerpMessageNetMessage;
			if (newMsg == null) return;

			LoadNetworkObject(newMsg.mob);

			if (NetworkObject == null) return;

			var getMob = NetworkObject;
			var mobMelee = getMob.GetComponent<MobMeleeAttack>();
			var mobAction = getMob.GetComponent<MobMeleeAction>();
			if (mobMelee == null & mobAction == null)
			{
				return;
			}

			if (mobMelee == null)
			{
				mobAction.ClientDoLerpAnimation(newMsg.dir);
				return;
			}

			mobMelee.ClientDoLerpAnimation(newMsg.dir);
		}

		public static MobMeleeLerpMessageNetMessage Send(GameObject mob, Vector2 dir)
		{
			//Only send to players in the area so that clients cannot snoop on mob positions
			//by watching debug log outputs from the process coroutine on this message

			MobMeleeLerpMessageNetMessage msg = new MobMeleeLerpMessageNetMessage
			{
				mob = mob.GetComponent<NetworkIdentity>().netId,
				dir = dir
			};

			new MobMeleeLerpMessage().SendToVisiblePlayers(mob.transform.position, msg);
			return msg;
		}
	}
}
