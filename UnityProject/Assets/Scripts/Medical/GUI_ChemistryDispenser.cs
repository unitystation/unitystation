using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GUI_ChemistryDispenser : NetTab
{
	[FormerlySerializedAs("HeaterTemperature")]
	public float HeaterTemperatureCelsius = 20;

	[FormerlySerializedAs("DispensedTemperature")]
	public float DispensedTemperatureCelsius = 30;

	public int DispensedNumber = 20;
	public bool HeaterOn = false;

	public ChemistryDispenser ChemistryDispenser;
	[SerializeField] private Chemistry.Reagent[] dispensableReagents = null;

	private List<string> DispenseAmounts = new List<string>()
	{
		//Some security bizz
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

	private NetUIElement<string> ListOfReagents
	{
		get
		{
			if (!listOfReagents)
			{
				listOfReagents = (NetUIElement<string>) this["IngredientList"];
			}

			return listOfReagents;
		}
	}

	//The thing that says 100U @ 10c
	private NetUIElement<string> totalAndTemperature;

	private NetUIElement<string> TotalAndTemperature
	{
		get
		{
			if (!totalAndTemperature)
			{
				totalAndTemperature = (NetUIElement<string>) this["AmountAndTemperature"];
			}

			return totalAndTemperature;
		}
	}

	void Start()
	{
		((NetUIElement<string>) this["20"]).SetValueServer("1");
		if (Provider != null)
		{
			//Makes sure it connects with the dispenser properly
			ChemistryDispenser = Provider.GetComponentInChildren<ChemistryDispenser>();
			//Subscribe to change event from ChemistryDispenser.cs
			ChemistryDispenser.changeEvent += UpdateAll;
			UpdateAll();
		}
	}

	//set how much it should dispense
	public void SetAddAmount(int Number)
	{
		DispensedNumber = Number;

		for (int i = 0; i < DispenseAmounts.Count; i++)
		{
			if (DispenseAmounts[i] == Number.ToString()
			) //Checks what button has been pressed  And sets the correct position appropriate
			{
				((NetUIElement<string>) this[DispenseAmounts[i]]).SetValueServer("1");
			}
			else
			{
				((NetUIElement<string>) this[DispenseAmounts[i]]).SetValueServer("0");
			}
		}

		//Logger.Log (DispensedNumber.ToString ());
		UpdateAll();
	}

	public void RemoveAmount(int Number)
	{
		if (ChemistryDispenser.Container != null)
		{
			//Logger.Log (Number.ToString ());
			ChemistryDispenser.Container.TakeReagents(Number);
		}

		UpdateAll();
	}

	public void DispenseChemical(Chemistry.Reagent reagent)
	{
		if (ChemistryDispenser.Container != null)
		{
			if (ChemistryDispenser.ThisState == PowerStates.On
			    || ChemistryDispenser.ThisState == PowerStates.LowVoltage
			    || ChemistryDispenser.ThisState == PowerStates.OverVoltage)
			{
				//Logger.Log (Chemical);
				if (dispensableReagents.Contains(reagent)) //Checks if the the dispenser can dispense this chemical
				{
					float OutDispensedNumber = 0;
					switch (ChemistryDispenser.ThisState)
					{
						case (PowerStates.OverVoltage):
							OutDispensedNumber = DispensedNumber * 2;
							break;
						case (PowerStates.LowVoltage):
							OutDispensedNumber = DispensedNumber * 0.5f;
							break;

						default:
							OutDispensedNumber = DispensedNumber;
							break;
					}

					ChemistryDispenser.Container.Add(new ReagentMix(reagent, OutDispensedNumber,
						DispensedTemperatureCelsius));
				}
			}
		}

		UpdateAll();
	}

	//Turns off and on the heater
	public void ToggleHeater()
	{
		HeaterOn = !HeaterOn;
		Logger.LogFormat("Heater turned {0}.", Category.Chemistry, HeaterOn ? "on" : "off");
		UpdateAll();
	}

	public void EjectContainer()
	{
		if (ChemistryDispenser.Container != null)
		{
			ChemistryDispenser.EjectContainer();
		}

		UpdateAll();
	}

	public void SetHeaterTemperature(string TheString)
	{
		if (int.TryParse(TheString, out var temp))
		{
			HeaterTemperatureCelsius = temp;
		}

		UpdateAll();
	}

	public void HeatingUpdate()
	{
		if (ChemistryDispenser.Container != null)
		{
			if (ChemistryDispenser.ThisState == PowerStates.On
			    || ChemistryDispenser.ThisState == PowerStates.LowVoltage
			    || ChemistryDispenser.ThisState == PowerStates.OverVoltage)
			{
				if (HeaterOn)
				{
					//Sets the temperature of the liquid. Could be more smooth/gradual change
					ChemistryDispenser.Container.Temperature = HeaterTemperatureCelsius;
				}
			}
		}
	}

	public void UpdateAll()
	{
		HeatingUpdate();
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

			TotalAndTemperature.SetValueServer($"{ChemistryDispenser.Container.ReagentMixTotal}U @ {(ChemistryDispenser.Container.Temperature)}°C");
		}
		else
		{
			newListOfReagents = "No reagents";
			TotalAndTemperature.SetValueServer("No container inserted");
		}

		ListOfReagents.SetValueServer(newListOfReagents);
	}

	public void OnDestroy()
	{
		//Unsubscribe container update event
		ChemistryDispenser.changeEvent -= UpdateAll;
	}
}
