using System;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnJoinTime : ProtipObject
	{
		private int lateTime = 1;
		[SerializeField] private ProtipSO lateTip;

		private void Start()
		{
			TriggerTip(GameManager.Instance.RoundTimeInMinutes >= lateTime ? lateTip : TipSO);
		}

		protected override bool TriggerConditions(GameObject triggeredBy, ProtipSO protipSo)
		{
			if (GameManager.Instance.CurrentRoundState is RoundState.Ended or RoundState.Restarting) return false;
			return base.TriggerConditions(triggeredBy, protipSo);
		}
	}
}