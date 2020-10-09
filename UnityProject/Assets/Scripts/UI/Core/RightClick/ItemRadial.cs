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

		[Tooltip("The label displayed in the center of the item radial.")]
		[SerializeField]
		private TMP_Text label = default;

		public override void Setup(int itemCount)
		{
			base.Setup(itemCount);
			label.SetText(string.Empty);
			UpdateArrows();
		}

		public void UpdateArrows()
		{
			previousArrow.SetActive(Mathf.Round(TotalRotation) > 0);
			nextArrow.SetActive(Mathf.Round(TotalRotation) < MaxIndex * ItemArcAngle);
		}

		public void ChangeLabel(string text) => label.SetText(text);
	}
}
