using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Research.BlastYieldDetector
{
	public class GUI_BlastGraphNode : DynamicEntry
	{
		private GUI_BlastYieldDetector blastGUI;
		public void Awake()
		{
			blastGUI = containedInTab.gameObject.GetComponent<GUI_BlastYieldDetector>();
		}
	}
}