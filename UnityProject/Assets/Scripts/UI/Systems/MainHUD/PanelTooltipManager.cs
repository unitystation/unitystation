using Learning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class PanelTooltipManager : MonoBehaviour
	{
		[SerializeField] private Text classicPanelTooltip;

		public void UpdateActiveTooltip(string tip)
		{
			classicPanelTooltip.text = tip;
		}
	}
}
