using System.Collections.Generic;
using US13.HealthV2.Living;
using US13.UI.Objects.Medical.genetics;

namespace US13.UI.Core.Net.Elements.Dynamic.Spawned
{
	public class MutationMiniGameList : EmptyItemList
	{

		public MutationMiniGameElement AddElement(BodyPartMutations.MutationRoundData.SliderParameters SliderParameters , MutationUnlockMiniGame MutationUnlockMiniGame) //data!!
		{
			var NewElement  = AddItem() as MutationMiniGameElement;
			NewElement.SetValues(SliderParameters, MutationUnlockMiniGame);
			return NewElement;
		}
	}
}
