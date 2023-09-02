using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Logs;
using Systems.Electricity;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Medical
{
	public class GUI_ChemistryDispenser : NetTab
	{
		public int DispensedNumber = 20;

		public ChemistryDispenser ChemistryDispenser;
		[SerializeField] private Reagent[] dispensableReagents = null;

		private List<string> DispenseAmounts = new List<string>()
		{
			// Some security bizz
			"5",
			"10",
			"15",
			"20",
			"25",
			"30",
			"50",
			"100",
		};

		private NetUIElement<string> listOfReagents;
		private NetUIElement<string> ListOfReagents => listOfReagents ??= (NetUIElement<string>)this["IngredientList"];

		// The thing that says 100U @ 10c
		private NetUIElement<string> totalAndTemperature;
		private NetUIElement<string> TotalAndTemperature => totalAndTemperature ??= (NetUIElement<string>)this["AmountAndTemperature"];

		private void Start()
		{
			((NetUIElement<string>)this["20"]).MasterSetValue("1");
			if (Provider != null)
			{
				// Makes sure it connects with the dispenser properly
				ChemistryDispenser = Provider.GetComponentInChildren<ChemistryDispenser>();
				// Subscribe to change event from ChemistryDispenser.cs
				ChemistryDispenser.changeEvent += UpdateAll;
				UpdateAll();
			}
		}

		// set how much it should dispense
		public void SetAddAmount(int Number)
		{
			DispensedNumber = Number;

			for (int i = 0; i < DispenseAmounts.Count; i++)
			{
				((NetUIElement<string>)this[DispenseAmounts[i]]).MasterSetValue(DispenseAmounts[i] == Number.ToString() ? "1": "0");
			}

			UpdateAll();
		}

		public void RemoveAmount(int Number)
		{
			if (ChemistryDispenser.Container != null)
			{
				ChemistryDispenser.Container.TakeReagents(Number);
			}

			UpdateAll();
		}

		public void DispenseChemical(Reagent reagent)
		{
			if (ChemistryDispenser.Container != null)
			{
				if (ChemistryDispenser.ThisState == PowerState.On
					|| ChemistryDispenser.ThisState == PowerState.LowVoltage
					|| ChemistryDispenser.ThisState == PowerState.OverVoltage)
				{
					if (dispensableReagents.Contains(reagent)) // Checks if the the dispenser can dispense this chemical
					{
						float OutDispensedNumber = 0; // TODO: is always 0? Hmm?
						switch (ChemistryDispenser.ThisState)
						{
							case PowerState.OverVoltage:
								OutDispensedNumber = DispensedNumber * 2;
								break;
							case PowerState.LowVoltage:
								OutDispensedNumber = DispensedNumber * 0.5f;
								break;

							default:
								OutDispensedNumber = DispensedNumber;
								break;
						}

						ChemistryDispenser.Container.Add(new ReagentMix(reagent, OutDispensedNumber,
							ChemistryDispenser.DispensedTemperatureCelsius));
					}
				}
			}

			UpdateAll();
		}

		// Turns off and on the heater
		public void ToggleHeater()
		{
			ChemistryDispenser.HeaterOn = !ChemistryDispenser.HeaterOn;
			ChemistryDispenser.UpdatePowerDraw();
			Loggy.LogFormat("Heater turned {0}.", Category.Chemistry, ChemistryDispenser.HeaterOn ? "on" : "off");
			UpdateAll();
		}

		public void EjectContainer(PlayerInfo player)
		{
			if (ChemistryDispenser.Container != null)
			{
				ChemistryDispenser.EjectContainer(player);
			}

			UpdateAll();
		}

		public void SetHeaterTemperature(string TheString)
		{
			if (int.TryParse(TheString, out var temp))
			{
				if (temp is < 0 or > 1000)
				{
					return;
				}

				ChemistryDispenser.HeaterTemperatureKelvin = temp;
			}

			UpdateAll();
		}


		public void UpdateAll()
		{
			UpdateDisplay();
		}

		// Updates UI elements
		public void UpdateDisplay()
		{
			var newListOfReagents = "";
			if (ChemistryDispenser.Container != null)
			{
				var roundedReagents = ChemistryDispenser.Container; // Round the contents to look better in the UI
				foreach (var reagent in roundedReagents)
				{
					newListOfReagents +=
						$"{char.ToUpper(reagent.Key.Name[0])}{reagent.Key.Name.Substring(1)} - {reagent.Value} U \n";
				}

				TotalAndTemperature.MasterSetValue($"{ChemistryDispenser.Container.ReagentMixTotal}U @ {(ChemistryDispenser.Container.Temperature)}°K");
			}
			else
			{
				newListOfReagents = "No reagents";
				TotalAndTemperature.MasterSetValue("No container inserted");
			}

			ListOfReagents.MasterSetValue(newListOfReagents);
		}

		public void OnDestroy()
		{
			// Unsubscribe container update event
			ChemistryDispenser.changeEvent -= UpdateAll;
		}
	}
}
