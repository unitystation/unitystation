using TMPro;
using UnityEngine;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public class LabeledGameKey : GameKey
	{
		[SerializeField] private TextMeshProUGUI Text;

		protected override void OnEnable()
		{
			base.OnEnable();
			if ( Text == null )
			{
				Text = GetComponentInChildren<TextMeshProUGUI>();
			}

			if ( Text )
			{
				Text.text = string.Join( "\n", Keys );
			}
		}
	}
}