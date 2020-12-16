using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using UnityEngine;
using UnityEngine.Serialization;
using Systems.Electricity;

namespace UI.Objects.Booze
{
	public class GUI_BoozeDispenser : NetTab
	{
		[NonSerialized]
		public int DispensedNumber = 20;

		[NonSerialized]
		public BoozeDispenser BoozeDispenser;

		[SerializeField]
		private Reagent[] dispensableReagents = null;

		private List<string> DispenseAmounts = new List<string>()
	{
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

		private NetUIElement<string> ListOfReagents {
			get {
				if (!listOfReagents)
				{
					listOfReagents = (NetUIElement<string>)this["IngredientList"];
				}

				return listOfReagents;
			}
		}

		private NetUIElement<string> quantity;

		private NetUIElement<string> QuantityList {
			get {
				if (!quantity)
				{
					quantity = (NetUIElement<string>)this["QuantityList"];
				}

				return quantity;
			}
		}

		private NetUIElement<string> total;

		private NetUIElement<string> Total {
			get {
				if (!total)
				{
					total = (NetUIElement<string>)this["Amount"];
				}

				return total;
			}
		}

		void Start()
		{
			((NetUIElement<string>)this["20"]).SetValueServer("1");
			if (Provider != null)
			{
				//Makes sure it connects with the dispenser properly
				BoozeDispenser = Provider.GetComponentInChildren<BoozeDispenser>();
				//Subscribe to change event from BoozeDispenser.cs
				BoozeDispenser.changeEvent += UpdateAll;
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
					((NetUIElement<string>)this[DispenseAmounts[i]]).SetValueServer("1");
				}
				else
				{
					((NetUIElement<string>)this[DispenseAmounts[i]]).SetValueServer("0");
				}
			}

			UpdateAll();
		}

		public void RemoveAmount(int Number)
		{
			if (BoozeDispenser.Container != null)
			{
				BoozeDispenser.Container.TakeReagents(Number);
			}

			UpdateAll();
		}

		public void DispenseChemical(Reagent reagent)
		{
			if (BoozeDispenser.Container != null)
			{
				if (BoozeDispenser.ThisState == PowerStates.On
					|| BoozeDispenser.ThisState == PowerStates.LowVoltage
					|| BoozeDispenser.ThisState == PowerStates.OverVoltage)
				{
					if (dispensableReagents.Contains(reagent)) //Checks if the the dispenser can dispense this chemical
					{
						float OutDispensedNumber = 0;
						switch (BoozeDispenser.ThisState)
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

						BoozeDispenser.Container.Add(new ReagentMix(reagent, OutDispensedNumber,18));
					}
				}
			}

			UpdateAll();
		}

		public void EjectContainer()
		{
			if (BoozeDispenser.Container != null)
			{
				BoozeDispenser.EjectContainer();
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
			string Reagents = "";
			string Quantitys = "";
			if (BoozeDispenser.Container != null)
			{
				StringBuilder newListOfReagents = new StringBuilder();
				StringBuilder newQuantityList = new StringBuilder();
				var reagentList = BoozeDispenser.Container;
				foreach (var reagent in reagentList)
				{
					newListOfReagents.AppendLine($"{char.ToUpper(reagent.Key.Name[0])}{reagent.Key.Name.Substring(1)}");
					newQuantityList.AppendLine($"{Math.Round(reagent.Value,1)}u");
				}
				Total.SetValueServer($"{BoozeDispenser.Container.ReagentMixTotal}/{BoozeDispenser.Container.MaxCapacity} Units");
				Reagents = newListOfReagents.ToString();
				Quantitys = newQuantityList.ToString();
			}
			else
			{
				Total.SetValueServer("No container inserted");
			}

			ListOfReagents.SetValueServer(Reagents);
			QuantityList.SetValueServer(Quantitys);
		}

		public void OnDestroy()
		{
			//Unsubscribe container update event
			BoozeDispenser.changeEvent -= UpdateAll;
		}
	}
}
