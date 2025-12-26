using System;
using Mirror;

namespace Actions.V2
{
	[Serializable]
	public class CooldownInfo : NetworkMessage
	{
		public string ActionId { get; private set; }
		public DateTime CooldownEnd { get; private set; }

		public CooldownInfo() { }
		public CooldownInfo(string actionId, DateTime cooldownEnd)
		{
			ActionId = actionId;
			CooldownEnd = cooldownEnd;
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.WriteString(ActionId);
			writer.WriteLong(CooldownEnd.Ticks);
		}

		public void Deserialize(NetworkReader reader)
		{
			ActionId = reader.ReadString();
			CooldownEnd = new DateTime(reader.ReadLong());
		}
	}
}