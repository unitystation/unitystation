using System.Collections.Generic;
using US13.UI.Core;
using US13.UI.Objects.Medical;

namespace US13.UI.Objects.MedBed
{
	public class ChemicalInjection_List : EmptyItemList
	{
		public GUI_ChemicalInjection AddElementReagent(US13.Objects.Medical.MedBed.ReagentReGenAndCap ReagentReGenAndCap,GUI_MedBed TParent)
		{
			var NewElement  = AddItem() as GUI_ChemicalInjection;
			NewElement.SetUp(ReagentReGenAndCap, TParent);
			return NewElement;
		}

		public List<GUI_ChemicalInjection> GetElements()
		{

			List<GUI_ChemicalInjection> ToReturn = new List<GUI_ChemicalInjection>();

			foreach (var Entry in Entries)
			{
				ToReturn.Add(Entry as GUI_ChemicalInjection);
			}

			return ToReturn;

		}
	}
}
