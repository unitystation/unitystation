using System.Collections.Generic;
using System.Text;
using Managers;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Objects.Wallmounts
{
	public class ClockWallmount : MonoBehaviour, IExaminable, IHoverTooltip
	{

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var report = new StringBuilder();
			report.AppendLine($"UST currently is: {InGameTimeManager.Instance.UniversalSpaceTime}");
			report.AppendLine($"UTC currently is: {InGameTimeManager.Instance.UtcTime}");
			return report.ToString();
		}

		public string HoverTip()
		{
			return null;
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			List<TextColor> interactions = new List<TextColor>();
			TextColor text = new TextColor
			{
				Text = "Shift+Left Click: Read time.",
				Color = IntentColors.Help
			};
			interactions.Add(text);
			return interactions;
		}
	}
}