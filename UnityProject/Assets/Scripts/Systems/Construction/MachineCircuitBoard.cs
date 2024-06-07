using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Machines
{
	/// <summary>
	/// Allows an object to function as a circuitboard for a computer, being placed into a computer frame and
	/// causing a particular computer to be spawned on completion.
	/// </summary>
	public class MachineCircuitBoard : MonoBehaviour, IHoverTooltip
	{
		[Tooltip("Machine parts scriptableobject; what gameobject to spawn, what parts needed")]
		[SerializeField]
		private MachineParts machineParts = null;

		/// <summary>
		/// What machine parts are needed
		/// </summary>
		public MachineParts MachinePartsUsed => machineParts;

		public void SetMachineParts(MachineParts MachineParts)
		{
			machineParts = MachineParts;
		}

		public string HoverTip()
		{
			if (machineParts == null) return null;
			var ingrediants = "Ingridents:\n";
			foreach (var machinePart in machineParts.machineParts)
			{
				ingrediants += $"{machinePart.itemTrait.Name} (x{machinePart.amountOfThisPart})\n".Color(Color.magenta);
			}
			return ingrediants;
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			return null;
		}
	}
}
