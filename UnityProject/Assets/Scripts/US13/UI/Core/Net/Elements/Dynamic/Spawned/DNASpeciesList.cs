using US13.Player;
using US13.UI.Objects.Medical;

namespace US13.UI.Core.Net.Elements.Dynamic.Spawned
{
	public class DNASpeciesList : EmptyItemList
	{
		public DNASpeciesElement AddElement(PlayerHealthData PlayerHealthData, GUI_DNAConsole GUI_DNAConsole)
		{
			var NewElement  = AddItem() as DNASpeciesElement;
			NewElement.SetValues(PlayerHealthData, GUI_DNAConsole);
			return NewElement;
		}
	}
}
