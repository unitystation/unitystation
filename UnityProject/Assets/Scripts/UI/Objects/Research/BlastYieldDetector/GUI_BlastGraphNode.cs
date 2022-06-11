using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Research
{
	public class GUI_BlastGraphNode : DynamicEntry
	{
		public void SetData()
		{
			transform.parent.parent.parent.parent.
				GetComponent<GUI_BlastYieldDetector>().SetData(transform.GetSiblingIndex());
		}
	}
}