using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.MobAIs
{
	public class MobController : MonoBehaviour
	{
		public List<MobObjective> MobObjectives = new List<MobObjective>();

		public void Awake()
		{
			MobObjectives = this.GetComponents<MobObjective>().ToList();
		}

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 0.85f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
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
				ChosenObjective.DoAction();
				ChosenObjective.Priority = 0;
			}
		}
	}
}