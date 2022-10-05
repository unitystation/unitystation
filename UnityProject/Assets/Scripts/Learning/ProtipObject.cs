using System;
using System.Collections;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Util;

namespace Learning
{
	public class ProtipObject : MonoBehaviour
	{
		public ProtipSO TipSO;
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
			if (TipSO == null)
			{
				Logger.LogError("[ProtipObject] - Component missing tip data.");
				Destroy(this);
				return;
			}
			if(CheckSaveStatus()) Destroy(this);
		}

		private bool CheckSaveStatus()
		{
			var saved = ProtipManager.Instance.ProtipSaveStates.Any(x =>
				x.Key == TipSO.TipTitle && x.Value == true);
			return saved;
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
			ProtipManager.Instance.QueueTip(TipSO);
			if (triggerOnce)
			{
				PlayerPrefs.SetString($"{TipSO.TipTitle}", "true");
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
			ProtipManager.Instance.QueueTip(TipSO);
			if (triggerOnce && ProtipManager.Instance.PlayerExperienceLevel > ProtipManager.ExperienceLevel.NewToSpaceStation)
			{
				ProtipManager.Instance.SaveTipState(protipSo.TipTitle);
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