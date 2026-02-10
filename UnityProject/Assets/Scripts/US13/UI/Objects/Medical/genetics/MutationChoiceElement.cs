using US13.HealthV2.Living;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Medical.genetics
{
	public class MutationChoiceElement : DynamicEntry
	{

		public MutationSO MutationSO;

		public MutationUnlockMiniGame MutationUnlockMiniGame;

		public NetText_label NetText_label;

		public void SetValues(MutationSO InMutationSO, MutationUnlockMiniGame InMutationUnlockMiniGame)
		{
			MutationSO = InMutationSO;
			MutationUnlockMiniGame = InMutationUnlockMiniGame;
			NetText_label.SetValue($"Difficulty 100/{BodyPartMutations.GetMutationRoundData(InMutationSO).ResearchDifficult}");
		}

		public void OnSelect()
		{
			MutationUnlockMiniGame.GenerateForMutation(MutationSO);
		}
	}
}
