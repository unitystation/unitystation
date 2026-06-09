using System.Collections.Generic;
using Shared.Managers;
using US13.Core.Utils;
using US13.Managers;
using Event = US13.Managers.Event;

namespace US13.Systems
{
	public class ClientSynchronisedEffectsManager : SingletonManager<ClientSynchronisedEffectsManager>
	{

		public Dictionary<uint, List<IClientSynchronisedEffect>> Data =
			new Dictionary<uint, List<IClientSynchronisedEffect>>();

		public static HashSet<uint>  CurrentlyOns =  new HashSet<uint>();

		private void OnEnable()
		{
			EventManager.AddHandler(Event.RoundEnded, ClearData);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundEnded, ClearData);
		}


		public void ClearData()
		{
			CurrentlyOns.Clear();
			Data.Clear();
		}

		public void ClientRegisterOnBody(uint BodyID, IClientSynchronisedEffect Effect)
		{
			if (Data.ContainsKey(BodyID) == false)
			{
				Data[BodyID] = new List<IClientSynchronisedEffect>();
			}

			Data[BodyID].Add(Effect);
		}

		public void ClientUnRegisterOnBody(uint BodyID, IClientSynchronisedEffect Effect)
		{
			if (Data.ContainsKey(BodyID) == false)
			{
				Data[BodyID] = new List<IClientSynchronisedEffect>();
			}

			if (Data[BodyID].Contains(Effect))
			{
				Data[BodyID].Remove(Effect);
			}
		}

		public void LeavingBody(uint BodyID)
		{
			if (CurrentlyOns.Contains(BodyID))
			{
				CurrentlyOns.Remove(BodyID);
			}

			if (Data.ContainsKey(BodyID))
			{
				foreach (var BodyValues in Data[BodyID])
				{
					BodyValues.ClientOnPlayerLeaveBody();
				}
			}
		}

		public void EnterBody(uint BodyID)
		{
			CurrentlyOns.Add(BodyID);
			if (Data.ContainsKey(BodyID))
			{
				foreach (var BodyValues in Data[BodyID])
				{
					BodyValues.ClientOnPlayerTransferProcess();
				}
			}
		}
	}
}
