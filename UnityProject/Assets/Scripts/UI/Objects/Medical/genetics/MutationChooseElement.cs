using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UI.Objects.Medical;
using UnityEngine;

public class MutationChooseElement : DynamicEntry
{

	public GUI_DNAConsole GUI_DNAConsole;
	public MutationSO MutationSO;

	public void SetValues(MutationSO InMutationSO, GUI_DNAConsole InGUI_DNAConsole)
	{
		MutationSO = InMutationSO;
		GUI_DNAConsole = InGUI_DNAConsole;
	}

	public void OnSelect()
	{
		GUI_DNAConsole.GenerateMutationTarget(MutationSO);
	}
}
