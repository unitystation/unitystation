using System.Text;
using UnityEngine;
using Systems.Electricity;
using Objects.Atmospherics;


namespace UI.Objects.Atmospherics
{
	public class GUI_Thermomachine : NetTab
	{
		[SerializeField]
		private NetLabel temperatureData = null;

		[SerializeField]
		private NetSlider onOffSwitch = null;

		private HeaterFreezer heaterFreezer;
		private HeaterFreezer HeaterFreezer {
			get {
				if (heaterFreezer == null)
					heaterFreezer = Provider.GetComponent<HeaterFreezer>();

				return heaterFreezer;
			}
		}

		public void OnTabOpenedHandler(ConnectedPlayer connectedPlayer)
		{
			var state = HeaterFreezer.ApcPoweredDevice.State == PowerState.Off ? "No Power" :
				HeaterFreezer.IsOn ? "On" : "Off";

			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"Status: {state}");
			stringBuilder.AppendLine($"Pipe Temp: {HeaterFreezer.CurrentTemperature}\n");
			stringBuilder.AppendLine($"Min Temp: {HeaterFreezer.MinTemperature}");
			stringBuilder.AppendLine($"Target Temp: {HeaterFreezer.TargetTemperature}");
			stringBuilder.AppendLine($"Max Temp: {HeaterFreezer.MaxTemperature}");

			temperatureData.Value = stringBuilder.ToString();

			onOffSwitch.Value = (HeaterFreezer.IsOn ? 1 * 100 : 0).ToString();
		}

		public void PowerChange()
		{
			HeaterFreezer.TogglePower(HeaterFreezer.IsOn == false);
		}

		public void Increase()
		{
			HeaterFreezer.ChangeTargetTemperature(1);
		}

		public void IncreaseTen()
		{
			HeaterFreezer.ChangeTargetTemperature(10);
		}

		public void Decrease()
		{
			HeaterFreezer.ChangeTargetTemperature(-1);
		}

		public void DecreaseTen()
		{
			HeaterFreezer.ChangeTargetTemperature(-10);
		}
	}
}
