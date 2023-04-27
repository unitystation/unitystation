using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Core.NetUI
{
	public class NetSliderDial : NetSlider
	{



		public override string Value {
			get => ((int)(TargetValue * 100)).ToString();
			protected set => SetTargetValue(int.Parse(value) / 100f);
		}

		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		public float StartValue = 0;
		public float CurrentValue = 0;
		public float TargetValue = 0;

		public float CurrentTime = 0;

		public void SetTargetValue(float To)
		{
			if (TargetValue != To)
			{
				TargetValue = To;
				CurrentTime = 0;
				StartValue = CurrentValue;
			}
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public void UpdateMe()
		{
			if (CurrentTime < 5)
			{
				CurrentTime += Time.deltaTime;
				float mainline = (1 + -Mathf.Exp(-5 * CurrentTime));
				float WobbleGrowth = (1 + -Mathf.Exp(0.5f * CurrentTime));
				float wobble = (Mathf.Sin(12 * CurrentTime) / 10);
				float wobbleDecay = (-Mathf.Exp(-0.9f * CurrentTime));

				CurrentValue = StartValue + (TargetValue - StartValue) * (mainline + (WobbleGrowth * wobble * wobbleDecay));
				Element.value = CurrentValue;
			}
		}

		public override void ExecuteServer(PlayerInfo subject) { }

		/// <summary>
		/// Server-only method for updating element (i.e. changing label text) from server GUI code
		/// </summary>
		public override void MasterSetValue(string value)
		{
			if (Value != value)
			{
				Value = value;
				UpdatePeepers();
			}
		}
	}
}
