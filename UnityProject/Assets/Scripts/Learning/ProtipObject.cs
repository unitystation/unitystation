using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using Util;

namespace Learning
{
	public class ProtipObject : MonoBehaviour
	{
		public ProtipSO TipSO;
		[SerializeField] private int saveID;
		[SerializeField] private bool triggerOnce = true;
		[SerializeField, ShowIf(nameof(triggerOnce))] private bool hasBeenTriggeredBefore = false;
		[SerializeField, HideIf(nameof(triggerOnce))] private float tipCooldown = 200f;
		private bool isOnCooldown = false;

		private void Awake()
		{
			var saved = PlayerPrefs.GetString($"{gameObject.GetComponent<PrefabTracker>().ForeverID}/{saveID.ToString()}");
			if(saved == "true" || TipSO == null) Destroy(this);
		}

		protected virtual bool TriggerConditions()
		{
			if (isOnCooldown) return false;
			if (hasBeenTriggeredBefore) return false;
			if (ProtipManager.Instance.PlayerExperienceLevel < TipSO.TipData.MinimumExperienceLevelToTrigger) return false;
			return true;
		}

		public void TriggerTip()
		{
			if(TriggerConditions() == false) return;
			ProtipManager.Instance.ShowTip(TipSO.TipData.Tip, TipSO.TipData.ShowDuration, TipSO.TipData.TipIcon, TipSO.TipData.ShowAnimation);
			if (triggerOnce)
			{
				PlayerPrefs.SetString($"{gameObject.GetComponent<PrefabTracker>().ForeverID}/{saveID.ToString()}", "true");
				PlayerPrefs.Save();
				return;
			}

			StartCoroutine(Cooldown());
		}

		public void TriggerTip(ProtipSO protipSo)
		{
			if(TriggerConditions() == false) return;
			ProtipManager.Instance.ShowTip(protipSo.TipData.Tip, protipSo.TipData.ShowDuration, protipSo.TipData.TipIcon, protipSo.TipData.ShowAnimation);
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