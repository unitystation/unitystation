using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_ChemistryDispenser : NetTab {

	public float HeaterTemperature = 20;
	public float DispensedTemperature = 30;
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
			Updateall ();
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
		Updateall ();
	} 
	public void RemoveAmount(int Number)
	{
		if (ChemistryDispenser.Container != null) {
			
			//Logger.Log (Number.ToString ());
			ChemistryDispenser.Container.MoveReagentsTo (Number);
		}
		Updateall ();
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

				ChemistryDispenser.Container.AddReagents (AddE, DispensedTemperature);
			}
		}
		Updateall ();
	}
		
	//Turns off and on the heater
	public void ToggleHeater()
	{
		HeaterOn = !HeaterOn;
		Logger.Log (HeaterOn.ToString());
		Updateall ();
	} 

	public void EjectContainer(){
		if (ChemistryDispenser.Container != null) 
		{
			//unhiding malarkey
			//Logger.Log ("Ejected");
			Vector3Int pos = ChemistryDispenser.ofthis.WorldPos ().RoundToInt();
			CustomNetTransform netTransform = ChemistryDispenser.objectse.GetComponent<CustomNetTransform>();
			netTransform.AppearAtPosition(pos);
			netTransform.AppearAtPositionServer(pos);
			ChemistryDispenser.Container = null;
			ChemistryDispenser.objectse = null;
		}
		Updateall ();
	}
	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
	public void SetHeaterTemperature(string TheString )
	{
		int mev;
		if (int.TryParse (TheString, out mev)) 
		{
			HeaterTemperature = int.Parse( TheString); 
		}

		Updateall ();
	}
	public void HeatingUpdate()
	{
		if (ChemistryDispenser.Container != null)
		{
			if (HeaterOn)
			{
				ChemistryDispenser.Container.Temperature = HeaterTemperature; //Sets the temperature of the liquid could be more smooth gradual change
			}
		}
	}
	public void Updateall()
	{ 
		HeatingUpdate ();
		UpdateDisplay ();
	}
	// Updates UI elements
	public void UpdateDisplay(){
		string newListOfReagents = "";
		if (ChemistryDispenser.Container != null) 
		{
			foreach (KeyValuePair<string,float> Chemical in ChemistryDispenser.Container.Contents) 
			{
				newListOfReagents = newListOfReagents + char.ToUpper (Chemical.Key [0]) + Chemical.Key.Substring (1) + " - " + Chemical.Value.ToString () + " U \n";
			}
			TotalAndTemperature.SetValue = ChemistryDispenser.Container.AmountOfReagents (ChemistryDispenser.Container.Contents).ToString () + " U @ " + ChemistryDispenser.Container.Temperature.ToString () + "C° ";
		}
		else 
		{
			newListOfReagents = "Current contain a nonexistent";
			TotalAndTemperature.SetValue = "No container inserted"; 
		}
		ListOfReagents.SetValue = newListOfReagents;
	}
}
