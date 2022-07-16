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

		[SerializeField, Tooltip("Incase there are more than one Protip on a single item/object")]
		private int saveID;
		[SerializeField, Tooltip("Does this tip object only trigger once then saved as so?")]
		private bool triggerOnce = true;

		[SerializeField]
		private bool showEvenAfterDeath = false;

		[SerializeField, HideIf(nameof(triggerOnce)), Tooltip("Will not appear again after a while to not annoy the player with it")]
		private float tipCooldown = 200f;

		private bool isOnCooldown = false;
		private bool hasBeenTriggeredBefore = false;

		private void Awake()
		{
			var saved = PlayerPrefs.GetString($"{gameObject.GetComponent<PrefabTracker>().ForeverID}/{saveID.ToString()}");
			if(saved == "true" || TipSO == null) Destroy(this);
		}

		protected virtual bool TriggerConditions(GameObject triggeredBy)
		{
			//To avoid issues with NREs, Protips should only trigger if a PlayerScript exists.
			if (PlayerManager.LocalPlayerScript == null) return false; 
			//triggeredBy check should only be null when you want a global protip incase of something like an event
			if (triggeredBy != null && triggeredBy != PlayerManager.LocalPlayerScript.gameObject) return false;
			if (isOnCooldown) return false;
			if (hasBeenTriggeredBefore) return false;
			if (showEvenAfterDeath == false && PlayerManager.LocalPlayerScript.IsDeadOrGhost) return false;
			if (ProtipManager.Instance.PlayerExperienceLevel > TipSO.TipData.MinimumExperienceLevelToTrigger) return false;
			return true;
		}

		public void TriggerTip(GameObject triggeredBy = null)
		{
			if(TriggerConditions(triggeredBy) == false) return;
			ProtipManager.Instance.ShowTip(TipSO.TipData.Tip, TipSO.TipData.ShowDuration, TipSO.TipData.TipIcon, TipSO.TipData.ShowAnimation);
			if (triggerOnce)
			{
				PlayerPrefs.SetString($"{gameObject.GetComponent<PrefabTracker>().ForeverID}/{saveID.ToString()}", "true");
				PlayerPrefs.Save();
				return;
			}

			StartCoroutine(Cooldown());
		}

		public void TriggerTip(ProtipSO protipSo, GameObject triggeredBy = null)
		{
			if(TriggerConditions(triggeredBy) == false) return;
			if(protipSo == null) 
			{
				Logger.LogError("Passed ProtipSO is null. Cannot trigger tip.");
				return;
			}
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