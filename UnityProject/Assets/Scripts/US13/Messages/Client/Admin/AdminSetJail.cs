using Logs;
using Mirror;
using US13.Core.Admin;
using US13.Core.Admin.Logs;
using US13.Core.Physics;
using US13.Managers;

namespace US13.Messages.Client.Admin
{
	public class AdminSetJail : ClientMessage<AdminSetJail.NetMessage>
	{

		public struct NetMessage : NetworkMessage
		{
			public bool Jail; //you RP too much jail.
			//your jokes too funny as a clown jail,
			//your jokes not funny enough as a clown jail,
			//We have the best players,
			//Because of jail.
			public string AccountID;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.PLAYER_MOVE))
			{
				if (AdminJail.AdminJailLocation == null)
				{
					Loggy.Error(" No jail found!! ");
					AdminLogsManager.AddNewLog(" No jail found!! ", LogCategory.Admin);
					return;
				}

				if (msg.Jail)
				{
					if (AdminJail.AdminJailLocation.JailedLocations.ContainsKey(msg.AccountID))
					{
						Loggy.Error("Already in jail");
					}
					else
					{
						if (PlayerList.Instance.TryGetByUserID(msg.AccountID, out PlayerInfo player))
						{
							var UniversalObjectPhysics = player.GameObject.GetComponent<UniversalObjectPhysics>();
							if (UniversalObjectPhysics == null) return;
							AdminJail.AdminJailLocation.JailedLocations[msg.AccountID] = UniversalObjectPhysics.OfficialPosition;
							UniversalObjectPhysics.AppearAtWorldPositionServer(AdminJail.AdminJailLocation.transform.position);
						}
					}
				}
				else
				{
					if (AdminJail.AdminJailLocation.JailedLocations.TryGetValue(msg.AccountID, out var location))
					{
						if (PlayerList.Instance.TryGetByUserID(msg.AccountID, out PlayerInfo player))
						{
							var UniversalObjectPhysics = player.GameObject.GetComponent<UniversalObjectPhysics>();
							if (UniversalObjectPhysics == null) return;

							UniversalObjectPhysics.AppearAtWorldPositionServer(location);
							AdminJail.AdminJailLocation.JailedLocations.Remove(msg.AccountID);
						}
					}
					else
					{
						Loggy.Error("Not in jail!");
					}
				}



			}
		}

		public static NetMessage Send(bool Jail, string AccountID)
		{
			NetMessage msg = new()
			{
				Jail = Jail,
				AccountID = AccountID
			};

			Send(msg);
			return msg;
		}
	}
}
