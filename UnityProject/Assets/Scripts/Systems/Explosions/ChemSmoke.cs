using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Chemistry;
using HealthV2;
using System.Threading.Tasks;
using Mirror;

namespace Chemistry
{
	public class ChemSmoke : MonoBehaviour
	{
		public float InitialLifetime = 3000;

		private ReagentMix reagents;

		public ReagentMix Reagents
		{
			get { return reagents; }
			set { if (reagents == null) reagents = value; SyncReagents(); }
		}

		private void SyncReagents()
        {
			if(reagents != null)
            {
				if (reagents.Total > 0)
				{
					gameObject.GetComponent<SpriteRenderer>().color = reagents.MixColor;
					DespawnAfter(InitialLifetime + reagents.Total * 1000);
					return;
				}
			}
			DespawnAfter(InitialLifetime);
		}

		public async Task DespawnAfter(float time)
		{
			await Task.Delay((int)time);
			Despawn.ServerSingle(gameObject);
		}
	}
}
