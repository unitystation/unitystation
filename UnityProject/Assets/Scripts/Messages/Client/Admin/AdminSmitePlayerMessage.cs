using Mirror;

namespace Assets.Scripts.Messages.Client.Admin
{

	/// <summary>
	///     Smite player message.  A tool for a admin to insta-kill a player.
	/// </summary>
	public class AdminSmitePlayerMessage : ClientMessage
	{
		public string Userid;
		public string AdminToken;
		public string UserToSmite;

		public override void Process()
		{
			var player = PlayerList.Instance.GetAdmin(Userid, AdminToken);
			if (player != null)
			{
				PlayerList.Instance.ProcessSmiteRequest(Userid, UserToSmite);
			}
		}

		/// <summary>
		/// Send a Smite message that will insta-kill a player.
		/// </summary>
		/// <param name="userId">UserId of the Admin performing the action</param>
		/// <param name="adminToken">Admin token of the Admin performing the action</param>
		/// <param name="userToSmite">UserId of the user that will be killed</param>
		/// <returns>The sent message</returns>

		public static AdminSmitePlayerMessage Send(string userId, string adminToken, string userToSmite)
		{
			AdminSmitePlayerMessage msg = new AdminSmitePlayerMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				UserToSmite = userToSmite
			};
			msg.Send();
			return msg;
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			Userid = reader.ReadString();
			AdminToken = reader.ReadString();
			UserToSmite = reader.ReadString();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteString(Userid);
			writer.WriteString(AdminToken);
			writer.WriteString(UserToSmite);
		}
	}
}
