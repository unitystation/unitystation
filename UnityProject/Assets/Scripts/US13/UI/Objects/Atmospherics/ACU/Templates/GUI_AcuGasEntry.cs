using UnityEngine;
using US13.Objects.Atmospherics;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;
using US13.UI.Objects.Atmospherics.ACU.Pages;

namespace US13.UI.Objects.Atmospherics.ACU.Templates
{
	/// <summary>
	/// An entry for the <see cref="GUI_AcuOverviewPage"/>, displaying the metrics for the associated gas.
	/// </summary>
	public class GUI_AcuGasEntry : DynamicEntry
	{
		[SerializeField]
		private NetText_label label = default;

		public void SetValues(string metricName, float ratio, float moles, AcuStatus molStatus)
		{
			var percentString = $"{ratio, 10:P}";
			var molString = GUI_Acu.ColorStringByStatus($"{moles, 8:N}", molStatus);

			label.MasterSetValue($"| {metricName, -18} | {percentString, -13} | {molString, -34} |");
			label.MasterSetValue($"| {metricName, -18} | {percentString, -13} | {molString, -34} |");
		}
	}
}
