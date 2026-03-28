using System.Collections.Generic;
using Mirror;
using US13.Core.Lighting;
using US13.Managers.NetworkManagement;
using Util;

namespace US13.Messages.Server
{
	public class UpdateLightBrightness : ServerMessage<UpdateLightBrightness.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int Voltage;
			public uint[] LightSourceIDs;
		}

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer) return;
			foreach (var LightID in msg.LightSourceIDs)
			{
				if (LightID is NetId.Invalid or NetId.Empty ) continue;

				CustomNetworkManager.Spawned[LightID].GetCachedComponent<LightSource>().BrightnessCalculation(msg.Voltage);
			}

		}

		public static NetMessage Send(int Voltage, List<LightSource> LightSources)
		{
			List<uint> NetIDs = new List<uint>();
			foreach (var LightSource in LightSources)
			{
				NetIDs.Add(LightSource  ? LightSource.netId : NetId.Invalid);
			}

			NetMessage  msg =
				new NetMessage
				{
					LightSourceIDs = NetIDs.ToArray(),
					Voltage =  Voltage
				};

			SendToAll(msg);
			return msg;
		}
	}
}
