using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Items.Storage;

namespace UI.Items
{
	public class GUI_PizzaBomb : NetTab
	{
		[SerializeField] private NetLabel status;
		[SerializeField] private NetToggle modeToggleButton;
		[SerializeField] private NetToggle armToggleButton;

		private float timerCount;
		private PizzaBox pizza;

		private void Start()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			pizza = Provider.GetComponent<PizzaBox>();
			pizza.PizzaGui = this;
			if (pizza.DetenationOnTimer)
			{
				StartCoroutine(UpdateTimer());
				yield break;
			}
			UpdateSignalStatusStatus();
		}

		public void ToggleArmMode()
		{
			if (armToggleButton.Value == "1")
			{
				pizza.IsArmed = true;
				if (pizza.DetenationOnTimer)
				{
					StartCoroutine(UpdateTimer());
					return;
				}
				UpdateSignalStatusStatus();
				return;
			}
			pizza.IsArmed = false;
			UpdateSignalStatusStatus();
		}

		public void ChangeMode()
		{
			if (modeToggleButton.Value == "1")
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
				return;
			}
			status.Value = DMMath.Prob(25f) ? "Ready to oga some bogas" : "Explosive Unarmed..";
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
			if (armToggleButton.Value == "1") return;
			pizza.TimeToDetonate += 1;
			StartCoroutine(UpdateTimer());
		}
		public void IncreaseTimeByTen()
		{
			if (armToggleButton.Value == "1") return;
			pizza.TimeToDetonate += 10;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByOne()
		{
			if ((pizza.TimeToDetonate - 1) <= 0 || armToggleButton.Value == "1") return;
			pizza.TimeToDetonate -= 1;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByTen()
		{
			if ((pizza.TimeToDetonate - 10) <= 0 || armToggleButton.Value == "1") return;
			pizza.TimeToDetonate -= 10;
			StartCoroutine(UpdateTimer());
		}
	}
}
