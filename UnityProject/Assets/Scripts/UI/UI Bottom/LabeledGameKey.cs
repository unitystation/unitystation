using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LabeledGameKey : GameKey
{
	private Text Text;

	protected override void OnEnable()
	{
		base.OnEnable();
		if ( Text == null )
		{
			Text = GetComponent<Text>();
		}

		if ( Text )
		{
			Text.text = string.Join( "\n", Keys );
		}
	}
}