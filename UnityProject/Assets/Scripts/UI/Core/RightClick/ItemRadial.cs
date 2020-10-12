using TMPro;
using UnityEngine;
using UI.Core.Radial;

namespace UI.Core.RightClick
{
	public class ItemRadial : ScrollableRadial<RightClickRadialButton>
	{
		[SerializeField]
		private CanvasRenderer previousArrow = default;

		[SerializeField]
		private CanvasRenderer nextArrow = default;

		[SerializeField]
		private TMP_Text itemLabel = default;

		public override void Setup(int itemCount)
		{
			base.Setup(itemCount);
			itemLabel.SetText(string.Empty);
			UpdateArrows();
		}

		public void UpdateArrows()
		{
			previousArrow.SetActive(Mathf.Round(TotalRotation) > 0);
			nextArrow.SetActive(Mathf.Round(TotalRotation) < MaxIndex * ItemArcMeasure);
		}

		public void ChangeLabel(string text) => itemLabel.SetText(text);
	}
}
