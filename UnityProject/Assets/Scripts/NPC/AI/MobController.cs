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

		public static int CurrentMobs = 0;

		public bool Active = false;

		public List<MobObjective> MobObjectives = new List<MobObjective>();


		void OnRoundRestart(Scene oldScene, Scene newScene)
		{
			CurrentMobs = 0;
		}

		public void Awake()
		{

			MobObjectives = this.GetComponents<MobObjective>().ToList();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;
			SceneManager.activeSceneChanged += OnRoundRestart;
			if (CurrentMobs > 20)
			{
				var mobError = " mob limit hit with  " + name + " Disabling new mobs";
				Logger.LogError(mobError);
				UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(mobError, null);
				return;
			}

			Active = true;
			CurrentMobs++;
			UpdateManager.Add(UpdateMe, 0.85f);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;
			SceneManager.activeSceneChanged -= OnRoundRestart;

			if (Active)
			{
				CurrentMobs--;
				Active = false;
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			}
		}

		public void UpdateMe()
		{
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
