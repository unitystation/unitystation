using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public partial class TestAction
{
	public bool ShowKeyInput => SpecifiedAction == ActionType.KeyInput;

	[AllowNesting] [ShowIf("ShowKeyInput")] public string Key;

	public void InitiateKeyInput()
	{
	}

}
