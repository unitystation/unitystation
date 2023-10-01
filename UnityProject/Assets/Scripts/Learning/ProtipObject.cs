using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Managers;
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
		private bool removeAfterRemembering = true;

		[SerializeField]
		private bool showEvenAfterDeath = false;

		[SerializeField]
		private float tipCooldown = 200f;

		private bool isOnCooldown = false;

		[SerializeField] protected List<string> highlightableObjectNames;

		protected virtual void Awake()
		{
			if (TipSO == null)
			{
				Loggy.LogError("[ProtipObject] - Component missing tip data.");
				RemoveThisComponent();
				return;
			}

			if (CheckSaveStatus()) RemoveThisComponent();
		}

		/// <summary>
		/// Ensures that we don't remove any components from the player gameObject.
		/// </summary>
		private void RemoveThisComponent()
		{
			if (PlayerManager.LocalPlayerScript == null) return;
			if (PlayerManager.LocalPlayerScript.gameObject == this.gameObject) return;
			Destroy(this);
		}

		private bool CheckSaveStatus()
		{
			var saved = ProtipManager.Instance.ProtipSaveStates.Any(x =>
				x.Key == TipSO.TipTitle && x.Value == true);
			return saved;
		}

		private bool CheckSaveStatus(ProtipSO protipSo)
		{
			var saved = ProtipManager.Instance.ProtipSaveStates.Any(x =>
				x.Key == protipSo.TipTitle && x.Value == true);
			return saved;
		}

		protected virtual bool TriggerConditions(GameObject triggeredBy, ProtipSO protipSo)
		{
			if (ProtipManager.Instance == null)
			{
				Loggy.LogError("[Protips] - UNABLE TO FIND PROTIP MANAGER!!!");
				return false;
			}
			if (isOnCooldown) return false;
			//To avoid issues with NREs, Protips should only trigger if a PlayerScript exists.
			if (PlayerManager.LocalPlayerScript == null) return false;
			//triggeredBy check should only be null when you want a global protip incase of something like an event
			if (triggeredBy != null && triggeredBy != PlayerManager.LocalPlayerScript.gameObject) return false;
			if (showEvenAfterDeath == false && PlayerManager.LocalPlayerScript.IsDeadOrGhost) return false;
			if (ProtipManager.Instance.PlayerExperienceLevel > protipSo.TipData.MinimumExperienceLevelToTrigger) return false;
			return true;
		}

		protected void TriggerTip(GameObject triggeredBy = null)
		{
			if (TriggerConditions(triggeredBy, TipSO) == false) return;
			ProtipManager.Instance.QueueTip(TipSO, highlightableObjectNames);
			if (triggerOnce)
			{
				ProtipManager.Instance.SaveTipState(TipSO.TipTitle);
				RemoveThisComponent();
				return;
			}

			StartCoroutine(Cooldown());
		}

		protected void TriggerTip(ProtipSO protipSo, GameObject triggeredBy = null, List<string> highlightNames = null)
		{
			if (TriggerConditions(triggeredBy, protipSo) == false && CheckSaveStatus(protipSo)) return;
			if (protipSo == null)
			{
				Loggy.LogError("Passed ProtipSO is null. Cannot trigger tip.");
				return;
			}
			ProtipManager.Instance.QueueTip(protipSo, highlightableObjectNames);
			if (triggerOnce)
			{
				ProtipManager.Instance.SaveTipState(protipSo.TipTitle);
				if (removeAfterRemembering) RemoveThisComponent();
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
