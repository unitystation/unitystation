using UnityEngine;

public class GUI_PDAMainMenu : NetPage
{
	/*[SerializeField]
	[Tooltip("CrewManifest here")]
	private GUI_PDA_CrewManifest manifestPage = null;  //The menuPage for reference*/

	[SerializeField] private GUI_PDA controller;

	[SerializeField] private NetLabel idLabel;

	[SerializeField] private NetLabel lightLabel;

	[SerializeField] private NetLabel machineLabel;

	//[SerializeField] [Tooltip("Put the subswitcher here")]
	//private NetPageSwitcher subSwitcher;

	public void UpdateId()
	{
		var idCard = controller.Pda.IdCard;
		var pdaName = controller.Pda.PdaRegisteredName;
		if (idCard != null && pdaName != null)
		{
			idLabel.Value = $"{idCard.RegisteredName}, {idCard.JobType}";
			var editedString = pdaName.Replace(" ", "_");
			machineLabel.Value = $"root/usr/home/{editedString}/Desktop";
		}
		else
		{
			if (idCard == null) idLabel.Value = "<No ID inserted>";
			if (pdaName == null) machineLabel.Value = "root/usr/home/guest/Desktop";
		}
	}

	public void SettingsPage()
	{
		controller.OpenSettings();
		UpdateId();
	}

	public void CrewManifestPage()
	{
		//subSwitcher.SetActivePage(manifestPage);
	}

	public void AtmosphericsPage()
	{
		//controller.OpenAtmospherics();
	}

	public void IdRemove()
	{
		controller.RemoveId();
		UpdateId();
	}

	public void ToggleFlashLight()
	{
		controller.Pda.ToggleFlashlight();
		// A condensed version of an if statement made by rider, basically it switches between off and on, pretty neato
		lightLabel.Value = controller.Pda.FlashlightOn ? "Flashlight (ON)" : "Flashlight (OFF)";
	}
}