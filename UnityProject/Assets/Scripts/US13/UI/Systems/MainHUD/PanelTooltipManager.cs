using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Systems.MainHUD
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
