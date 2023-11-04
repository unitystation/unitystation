using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Research
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