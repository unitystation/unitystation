using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Chemistry;
using HealthV2;
using Items;
using System.Threading.Tasks;
using Mirror;

namespace Chemistry
{
	public class ChemFoam : MonoBehaviour, IServerDespawn //smoke copypasta
	{
		public int InitialLifetime = 10000;

		public float UpdateRate = 2.5f;

		public float ReagentMultiplier = 100; //since initial reagents in normal conditions have small volume, rendering foam useless

		private ReagentMix reagents;

		public ReagentMix Reagents
		{
			get { return reagents; }
			set { if (reagents == null && value != null) reagents = value; SyncReagents(); }
		}

		[NonSerialized]
		public Matrix Matrix;

		public void OnDespawnServer(DespawnInfo info)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void SyncReagents()
		{
			if (reagents != null)
			{
				if (reagents.Total > 0)
				{
					gameObject.GetComponent<SpriteRenderer>().color = reagents.MixColor;
					reagents.Multiply(ReagentMultiplier);
					DespawnAfter(InitialLifetime + (int)reagents.Total * 1000);
					UpdateManager.Add(UpdateMe, UpdateRate, offsetUpdate: false);
					return;
				}
			}
			DespawnAfter(InitialLifetime);
		}

		private void UpdateMe()
		{
			var players = Matrix.Get<PlayerScript>(gameObject.GetComponent<Transform>().position.RoundToInt(), true);
			foreach (PlayerScript player in players)
			{
				if (player.playerHealth.IsDead == false)
				{
					player.gameObject.GetComponent<CirculatorySystemBase>().BloodPool.Add(reagents); //yes, we are duping reagents
					Logger.Log(player.gameObject.GetComponent<CirculatorySystemBase>().BloodPool.ToString());

				}
			}
		}

		public async Task DespawnAfter(int time)
		{
			await Task.Delay(time);
			Despawn.ServerSingle(gameObject);
		}
	}
}
