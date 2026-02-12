using System.Collections.Generic;
using US13.Objects.Medical;
using US13.UI.Objects.Medical;

namespace US13.UI.Core.Net.Elements.Dynamic.Spawned
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
