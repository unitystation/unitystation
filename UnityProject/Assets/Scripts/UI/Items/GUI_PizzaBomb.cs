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
			if (pizza.DetonateByTimer)
			{
				StartCoroutine(UpdateTimer());
				yield break;
			}
			UpdateSignalStatusStatus();
		}

		public void ToggleArmMode()
		{
			pizza.IsArmed = !pizza.IsArmed;
			if (pizza.IsArmed && pizza.DetonateByTimer)
			{
				StartCoroutine(UpdateTimer());
				UpdateSignalStatusStatus();
				return;
			}

			UpdateSignalStatusStatus();
		}

		public void ChangeMode()
		{
			if (modeToggleButton.Value == "1")
			{
				UpdateSignalStatusStatus();
				return;
			}
			status.SetValueServer(DisplayTime());
		}

		private void UpdateSignalStatusStatus()
		{
			if (pizza.IsArmed)
			{
				status.SetValueServer("Armed");
				return;
			}
			status.SetValueServer(DMMath.Prob(25f) ? "Ready to oga some bogas" : "Explosive Unarmed..");
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
				status.SetValueServer(DisplayTime());
				yield break;
			}
			while (timerCount > 0)
			{
				timerCount -= 1;
				status.SetValueServer(DisplayTime());
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
