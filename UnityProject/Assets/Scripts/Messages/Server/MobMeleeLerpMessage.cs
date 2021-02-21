using System.Collections;
using Mirror;
using UnityEngine;

namespace Systems.MobAIs
{
	public class MobMeleeLerpMessage : ServerMessage
	{
		public struct MobMeleeLerpMessageNetMessage : NetworkMessage
		{
			public uint mob;
			public Vector2 dir;
		}

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public MobMeleeLerpMessageNetMessage message;

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as MobMeleeLerpMessageNetMessage?;
			if(newMsgNull == null) return;
			var newMsg = newMsgNull.Value;

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
