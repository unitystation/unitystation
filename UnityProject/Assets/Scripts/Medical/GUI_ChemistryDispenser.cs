using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GUI_ChemistryDispenser : NetTab {

	[FormerlySerializedAs("HeaterTemperature")]
	public float HeaterTemperatureCelsius = 20;

	[FormerlySerializedAs("DispensedTemperature")]
	public float DispensedTemperatureCelsius = 30;

	public int DispensedNumber = 20;
	public bool HeaterOn = false;

	public ChemistryDispenser ChemistryDispenser;
	private HashSet<string> DispensableChemicals = new HashSet<string>(){ //Some security bizz
		"aluminium",
		"bromine",
		"carbon",
		"chlorine",
		"copper",
		"ethanol",
		"fluorine",
		"hydrogen",
		"iodine",
		"iron",
		"lithium",
		"mercury",
		"nitrogen",
		"oxygen",
		"phosphorus",
		"potassium",
		"radium",
		"sacid",
		"silicon",
		"silver",
		"sodium",
		"stable_plasma",
		"sugar",
		"sulfur",
		"water",
		"welding_fuel",
		"cleaner"
	};

	private List<string> DispenseAmounts = new List<string>(){ //Some security bizz
		"5",
		"10",
		"15",
		"20",
		"25",
		"30",
		"50",
		"100",
	};

	private NetUIElement listOfReagents;
	private NetUIElement ListOfReagents {
		get {
			if ( !listOfReagents ) {
				listOfReagents = this["IngredientList"];
			}
			return listOfReagents;
		}
	}

	//The thing that says 100U @ 10c
	private NetUIElement totalAndTemperature;
	private NetUIElement TotalAndTemperature {
		get {
			if ( !totalAndTemperature ) {
				totalAndTemperature = this["AmountAndTemperature"];
			}
			return totalAndTemperature;
		}
	}

	void Start()
	{
		this ["20"].SetValue = "1";
		if (Provider != null)
		{
			//Makes sure it connects with the dispenser properly
			ChemistryDispenser = Provider.GetComponentInChildren<ChemistryDispenser> ();
			//Subscribe to change event from ChemistryDispenser.cs
			ChemistryDispenser.changeEvent += UpdateAll;
			UpdateAll ();
		}

	}

	//set how much it should dispense
	public void SetAddAmount(int Number)
	{
		DispensedNumber = Number;

		for (int i = 0; i < DispenseAmounts.Count; i++)
		{
			if (DispenseAmounts [i] == Number.ToString ()) //Checks what button has been pressed  And sets the correct position appropriate
			{
				this [DispenseAmounts [i]].SetValue = "1";
			} else {
				this [DispenseAmounts [i]].SetValue = "0";
			}
		}

		//Logger.Log (DispensedNumber.ToString ());
		UpdateAll ();
	}
	public void RemoveAmount(int Number)
	{
		if (ChemistryDispenser.Container != null) {

			//Logger.Log (Number.ToString ());
			ChemistryDispenser.Container.TakeReagents(Number);
		}
		UpdateAll ();
	}

	public void DispenseChemical(string Chemical )
	{
		if (ChemistryDispenser.Container != null)
		{
			//Logger.Log (Chemical);
			if (DispensableChemicals.Contains (Chemical)) //Checks if the the dispenser can dispense this chemical
			{
				Dictionary<string,float> AddE = new Dictionary<string,float> ()
				{
					[Chemical] = DispensedNumber
				};

				ChemistryDispenser.Container.AddReagentsCelsius (AddE, DispensedTemperatureCelsius);
			}
		}
		UpdateAll ();
	}

	//Turns off and on the heater
	public void ToggleHeater()
	{
		HeaterOn = !HeaterOn;
		Logger.LogFormat("Heater turned {0}.", Category.Chemistry, HeaterOn ? "on" : "off");
		UpdateAll ();
	}

	public void EjectContainer(){
		if (ChemistryDispenser.Container != null)
		{
			ChemistryDispenser.EjectContainer();
		}
		UpdateAll ();
	}
	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
	public void SetHeaterTemperature(string TheString )
	{
		if (int.TryParse (TheString, out var temp))
		{
			HeaterTemperatureCelsius = temp;
		}

		UpdateAll ();
	}
	public void HeatingUpdate()
	{
		if (ChemistryDispenser.Container != null)
		{
			if (HeaterOn)
			{
				//Sets the temperature of the liquid. Could be more smooth/gradual change
				ChemistryDispenser.Container.TemperatureCelsius = HeaterTemperatureCelsius;
			}
		}
	}
	public void UpdateAll()
	{
		HeatingUpdate ();
		UpdateDisplay ();
	}
	// Updates UI elements
	public void UpdateDisplay(){
		string newListOfReagents = "";
		if (ChemistryDispenser.Container != null)
		{
			var roundedReagents = Calculations.RoundReagents(ChemistryDispenser.Container.Contents); // Round the contents to look better in the UI
			foreach (KeyValuePair<string,float> Chemical in roundedReagents)
			{
				newListOfReagents =
					$"{newListOfReagents}{char.ToUpper(Chemical.Key[0])}{Chemical.Key.Substring(1)} - {Chemical.Value} U \n";
			}
			TotalAndTemperature.SetValue =
				$"{ChemistryDispenser.Container.AmountOfReagents(roundedReagents)}U @ {(ChemistryDispenser.Container.TemperatureCelsius)}°C";
		}
		else
		{
			newListOfReagents = "No reagents";
			TotalAndTemperature.SetValue = "No container inserted";
		}
		ListOfReagents.SetValue = newListOfReagents;
	}
	public void OnDestroy()
	{
		//Unsubscribe container update event
		ChemistryDispenser.changeEvent -= UpdateAll;
	}
}
