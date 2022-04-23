using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using Util;

namespace Learning
{
	public class ProtipObject : MonoBehaviour
	{
		public Protip Tip;
		[SerializeField] private int saveID;
		[SerializeField] private bool triggerOnce = true;
		[SerializeField, ShowIf(nameof(triggerOnce))] private bool hasBeenTriggeredBefore = false;
		[SerializeField, HideIf(nameof(triggerOnce))] private float tipCooldown = 200f;
		private bool isOnCooldown = false;

		private void Awake()
		{
			var saved = PlayerPrefs.GetString($"{gameObject.GetComponent<PrefabTracker>().ForeverID}/{saveID.ToString()}");
			if(saved == "true") Destroy(this);
		}

		protected virtual bool TriggerConditions()
		{
			if (hasBeenTriggeredBefore) return false;
			if (ProtipManager.Instance.PlayerExperienceLevel < Tip.MinimumExperienceLevelToTrigger) return false;
			return true;
		}

		public void TriggerTip()
		{
			if(TriggerConditions() == false) return;
			ProtipManager.Instance.ShowTip(Tip.Tip, Tip.ShowDuration, Tip.TipIcon, Tip.ShowAnimation);
			if (triggerOnce)
			{
				PlayerPrefs.SetString($"{gameObject.GetComponent<PrefabTracker>().ForeverID}/{saveID.ToString()}", "true");
				PlayerPrefs.Save();
				return;
			}

			StartCoroutine(Cooldown());
		}

		private IEnumerator Cooldown()
		{
			isOnCooldown = true;
			yield return WaitFor.Seconds(tipCooldown);
			isOnCooldown = false;
		}
	}
}