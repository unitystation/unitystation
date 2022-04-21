using System;
using NaughtyAttributes;
using UnityEngine;

namespace Learning
{
	public class ProtipObject : MonoBehaviour
	{
		public Protip Tip;

		public void Awake()
		{
			if (Tip == null)
			{
				Logger.LogError($"A Protip component has been added to [{gameObject.name}] but there was no tip data on it!");
				return;
			}
		}

		protected virtual bool TriggerConditions()
		{
			return false;
		}

		public void TriggerTip()
		{
			if(TriggerConditions() == false) return;
			ProtipManager.Instance.ShowTip(Tip.Tip, Tip.ShowDuration, Tip.TipIcon, Tip.ShowAnimation);
		}
	}
}