using US13.HealthV2.Living;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Medical.genetics
{
	public class MutationChooseElement : DynamicEntry
	{

		public NetText_label NetText_label;
		public GUI_DNAConsole GUI_DNAConsole;
		public MutationSO MutationSO;

		public void SetValues(MutationSO InMutationSO, GUI_DNAConsole InGUI_DNAConsole)
		{
			MutationSO = InMutationSO;
			GUI_DNAConsole = InGUI_DNAConsole;
			NetText_label.MasterSetValue(InMutationSO.DisplayName);
		}

		public void OnSelect()
		{
			GUI_DNAConsole.GenerateMutationTarget(MutationSO);
		}
	}
}
