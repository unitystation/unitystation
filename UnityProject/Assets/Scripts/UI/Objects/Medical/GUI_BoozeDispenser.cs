using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UI.Core.NetUI;
using Chemistry;
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

		private readonly List<string> DispenseAmounts = new List<string>
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
		private NetUIElement<string> ListOfReagents => listOfReagents ??= (NetUIElement<string>) this["IngredientList"];

		private NetUIElement<string> quantity;
		private NetUIElement<string> QuantityList => quantity ??= (NetUIElement<string>)this["QuantityList"];

		private NetUIElement<string> total;
		private NetUIElement<string> Total => total ??= (NetUIElement<string>)this["Amount"];

		private void Start()
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

		// set how much it should dispense
		public void SetAddAmount(int Number)
		{
			DispensedNumber = Number;

			for (int i = 0; i < DispenseAmounts.Count; i++)
			{
				// Checks what button has been pressed and sets the correct position appropriate
				((NetUIElement<string>)this[DispenseAmounts[i]])
						.SetValueServer(DispenseAmounts[i] == Number.ToString() ? "1" : "0");
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
				if (BoozeDispenser.ThisState == PowerState.On
					|| BoozeDispenser.ThisState == PowerState.LowVoltage
					|| BoozeDispenser.ThisState == PowerState.OverVoltage)
				{
					if (dispensableReagents.Contains(reagent)) //Checks if the the dispenser can dispense this chemical
					{
						var OutDispensedNumber = BoozeDispenser.ThisState switch
						{
							PowerState.OverVoltage => DispensedNumber * 2,
							PowerState.LowVoltage => DispensedNumber * 0.5f,
							_ => DispensedNumber,
						};
						BoozeDispenser.Container.Add(new ReagentMix(reagent, OutDispensedNumber,18));
					}
				}
			}

			UpdateAll();
		}

		public void EjectContainer(PlayerInfo player)
		{
			if (BoozeDispenser.Container != null)
			{
				BoozeDispenser.EjectContainer(player);
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
