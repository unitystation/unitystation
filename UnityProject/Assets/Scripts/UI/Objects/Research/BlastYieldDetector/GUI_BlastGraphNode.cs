using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Research
{
	public class GUI_BlastGraphNode : DynamicEntry
	{
		private GUI_BlastYieldDetector blastGUI;
		public void Awake()
		{
			blastGUI = MasterTab.gameObject.GetComponent<GUI_BlastYieldDetector>();
		}

		public void SetData()
		{
			blastGUI.SetCurrentShownData(transform.GetSiblingIndex());
		}
	}
}