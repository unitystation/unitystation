﻿using UnityEngine;
using System.Collections;
using System.Data;
using Items.Storage;
using UnityEngine.UI;

namespace UI.Objects
{
	public class GUI_PizzaBomb : NetTab
	{
		[SerializeField] private NetLabel status;
		[SerializeField] private NetToggle modeToggleButton;
		[SerializeField] private NetToggle armToggleButton;

		[SerializeField] private Color safeColor = Color.green;
		[SerializeField] private Color dangerColor = Color.red;

		private float timerCount;
		private PizzaBox pizza;

		private void Start()
		{
			StartCoroutine(WaitForProvider());
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			pizza = Provider.GetComponent<PizzaBox>();
			pizza.GUI = this;
			if (pizza.DetenationOnTimer)
			{
				StartCoroutine(UpdateTimer());
				yield break;
			}
			UpdateSignalStatusStatus();
		}


		public void HideUI()
		{
			gameObject.SetActive(false);
		}

		public void ToggleArmMode()
		{
			if (armToggleButton.Element.isOn)
			{
				pizza.IsArmed = true;
				if (pizza.DetenationOnTimer)
				{
					StartCoroutine(UpdateTimer());
					return;
				}
				UpdateSignalStatusStatus();
			}
		}
		public void ChangeMode()
		{
			if (modeToggleButton.Element.isOn)
			{
				UpdateSignalStatusStatus();
				return;
			}
			status.Value = DisplayTime();
		}

		private void UpdateSignalStatusStatus()
		{
			if (pizza.IsArmed)
			{
				status.Value = "Awaiting Signal..";
				status.Element.color = dangerColor;
				return;
			}
			status.Value = DMMath.Prob(25f) ? "Ready to oga some bogas" : "Explosive Unarmed..";
			status.Element.color = safeColor;
		}

		private string DisplayTime()
		{
			return $"{Mathf.RoundToInt(timerCount / 60)}:{(timerCount % 60).RoundToLargestInt()}";
		}

		public IEnumerator UpdateTimer()
		{
			timerCount = pizza.TimeToDetonate;
			if (pizza.BombIsCountingDown == false)
			{
				status.Value = DisplayTime();
				yield break;
			}
			while (timerCount > 0)
			{
				timerCount -= 1;
				status.Value = DisplayTime();
				yield return WaitFor.Seconds(1f);
			}
		}

		public void IncreaseTimeByOne()
		{
			if(armToggleButton.Element.isOn) return;
			pizza.TimeToDetonate += 1;
			StartCoroutine(UpdateTimer());
		}
		public void IncreaseTimeByTen()
		{
			if(armToggleButton.Element.isOn) return;
			pizza.TimeToDetonate += 10;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByOne()
		{
			if((pizza.TimeToDetonate - 1) <= 0 || armToggleButton.Element.isOn) return;
			pizza.TimeToDetonate -= 1;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByTen()
		{
			if((pizza.TimeToDetonate - 10) <= 0 || armToggleButton.Element.isOn) return;
			pizza.TimeToDetonate -= 10;
			StartCoroutine(UpdateTimer());
		}

	}
}