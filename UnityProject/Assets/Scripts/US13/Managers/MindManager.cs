using System.Collections.Generic;
using Shared.Managers;
using US13.Player;
using US13.UI.Systems.AdminTools;

namespace US13.Managers
{
	public class MindManager : SingletonManager<MindManager>
	{

		public Dictionary<uint, Mind> minds = new Dictionary<uint, Mind>();

		public uint MindID;


		public  List<AdminMindEntryData> GetMindStates()
		{

			List<AdminMindEntryData> Minds = new List<AdminMindEntryData>();

			foreach (var mind in minds)
			{
				Minds.Add(mind.Value.GetAdminMindEntryData());
			}


			return Minds;
		}

	}
}
