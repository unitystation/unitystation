using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Behaviours.Meta
{
	public class SystemManager: NetworkBehaviour
	{
		private List<SystemBehaviour> systems = new List<SystemBehaviour>();
		
		public override void OnStartServer()
		{
			Initialize();
		}

		private void Initialize()
		{
			for (int i = 0; i < systems.Count; i++)
			{
				systems[i].Initialize();
			}
		}
		
		public void Register(SystemBehaviour system)
		{
			systems.Add(system);
		}

		public void UpdateAt(Vector3Int position)
		{
			for (int i = 0; i < systems.Count; i++)
			{
				systems[i].UpdateAt(position);
			}
		}
	}
}