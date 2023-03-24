using System;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnJoinTime : ProtipObject
	{
		[SerializeField] private ProtipSO lateTip;

		private const int LATE_TIME = 1;

		private void Start()
		{
			TriggerTip(GameManager.Instance.RoundTimeInMinutes >= LATE_TIME ? lateTip : TipSO);
		}

		protected override bool TriggerConditions(GameObject triggeredBy, ProtipSO protipSo)
		{
			if (GameManager.Instance.CurrentRoundState is RoundState.Ended or RoundState.Restarting) return false;
			return base.TriggerConditions(triggeredBy, protipSo);
		}
	}
}