using System.Collections.Generic;
using Objects.Machines;
using Objects.Medical;
using UI.Core.NetUI;
using UI.Objects.Cargo;
using UI.Objects.Medical;
using UI.Objects.Medical.Cloning;
using UnityEngine;

namespace UI.Core.Net.Elements.Dynamic.Spawned
{
	public class DNAStrandList : EmptyItemList
	{

		public bool HasEntryInArea(DNAStrandElement.Location Location)
		{
			var Elements = GetElements();
			foreach (var Entry in Elements)
			{
				if (Entry.NetParentSetter.Value == (int) Location)
				{
					return true;
				}
			}

			return false;
		}



		public DNAStrandElement AddElement(DNAMutationData.DNAPayload Payload, string target, DNAStrandElement.Location SetLocation)
		{
			var NewElement  = AddItem() as DNAStrandElement;
			NewElement.SetValues(Payload,target, SetLocation );
			return NewElement;
		}

		public List<DNAStrandElement> GetElements()
		{

			List<DNAStrandElement> ToReturn = new List<DNAStrandElement>();

			foreach (var Entry in Entries)
			{
				ToReturn.Add(Entry as DNAStrandElement);
			}

			return ToReturn;

		}

	}
}
