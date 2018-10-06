using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


	public class SystemManager: NetworkBehaviour
	{
		private List<SystemBehaviour> systems = new List<SystemBehaviour>();
		private bool initialized = false;
		
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

			initialized = true;
		}
		
		public void Register(SystemBehaviour system)
		{
			systems.Add(system);
		}

		public void UpdateAt(Vector3Int position)
		{
			if ( !initialized )
			{
				return;
			}
			for (int i = 0; i < systems.Count; i++)
			{
				systems[i].UpdateAt(position);
			}
		}
	}
