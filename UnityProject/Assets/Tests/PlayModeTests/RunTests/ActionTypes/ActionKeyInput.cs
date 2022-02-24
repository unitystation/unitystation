using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

public partial class TestAction
{
	public bool ShowKeyInput => SpecifiedAction == ActionType.KeyInput;

	[AllowNesting] [ShowIf(nameof(ShowKeyInput))] public KeyInput DataKeyInput;



	[System.Serializable]
	public class KeyInput
	{
		public bool UnPress;
		public KeyCode Key;
		public bool InitiateKeyInput(TestRunSO TestRunSO)
		{
			if (UnPress)
			{
				InputManagerWrapper.UnPressKey(Key);
			}
			else
			{
				InputManagerWrapper.PressKey(Key);
			}

			return true;
		}
	}

	public bool InitiateKeyInput(TestRunSO TestRunSO)
	{
		return DataKeyInput.InitiateKeyInput(TestRunSO);
	}

}
