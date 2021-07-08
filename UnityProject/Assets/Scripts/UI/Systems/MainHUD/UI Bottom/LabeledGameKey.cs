using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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