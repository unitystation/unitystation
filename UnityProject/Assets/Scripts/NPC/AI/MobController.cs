using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems.MobAIs
{
	public class MobController : MonoBehaviour
	{
		public RegisterTile RegisterTile;

		private int PlayerMask;
		public bool Active = false;

		public List<MobObjective> MobObjectives = new List<MobObjective>();

		public static readonly float UpdateTimeInterval = 0.85f;


		public void Awake()
		{
			PlayerMask = LayerMask.GetMask("Players");
			MobObjectives = this.GetComponents<MobObjective>().ToList();
			RegisterTile = this.GetComponent<RegisterTile>();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;
			UpdateManager.Add(UpdateMe, UpdateTimeInterval);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		public Collider2D[] results = new Collider2D[100];

		public void UpdateMe()
		{
			if (RegisterTile.Matrix.PresentPlayers.Count == 0) return;

			foreach (var _MobObjective in MobObjectives)
			{
				_MobObjective.ContemplatePriority();
			}

			MobObjective ChosenObjective = null;

			foreach (var _MobObjective in MobObjectives)
			{
				if (ChosenObjective == null)
				{
					ChosenObjective = _MobObjective;
				}
				else
				{
					if (_MobObjective.Priority > ChosenObjective.Priority)
					{
						ChosenObjective = _MobObjective;
					}
				}
			}

			if (ChosenObjective != null)
			{
				ChosenObjective.TryAction();
				ChosenObjective.Priority = 0;
			}
		}
	}
}