using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Hacking : NetTab
{
	IHackable hackInterface;
	HackingProcessBase hackProcess;

    void Start()
    {
		if (Provider != null)
		{
			//Makes sure it connects with the dispenser properly
			hackInterface = Provider.GetComponentInChildren<IHackable>();
			hackProcess = Provider.GetComponentInChildren<HackingProcessBase>();
			hackProcess.RegisterHackingGUI(this);
			
		}
	}

}
