using System;
using System.Collections;
using System.Collections.Generic;
using UI.Objects.Medical;
using UnityEngine;

public class GUI_PillChemProductEntry : MonoBehaviour
{

	public int index = -1;

	public GUI_ChemMaster GUI_ChemMaster;


	public void ButtonSelect()
	{
		GUI_ChemMaster.PillSelectionArea.MasterNetSetActive(false);
		GUI_ChemMaster.PillChosen(index);
	}

}
