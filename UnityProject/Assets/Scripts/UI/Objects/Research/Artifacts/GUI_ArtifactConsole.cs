using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using System;
using Systems.Research.Objects;
using Items.Science;
using Objects.Research;
using ScriptableObjects.Systems.Research;
using Items.Storage.VirtualStorage;
using Mirror;
using UnityEngine.UI;


namespace UI.Objects.Research
{
	public class GUI_ArtifactConsole : NetTab
	{
		[SerializeField]
		private Dropdown appearanceDropdown;
		[SerializeField]
		private InputFieldFocus radInput;
		[SerializeField]
		private InputFieldFocus bluespaceInput;
		[SerializeField]
		private InputFieldFocus bananiumInput;

		[SerializeField]
		private Dropdown interactEffectDropdown;
		[SerializeField]
		private Dropdown areaEffectDropdown;
		[SerializeField]
		private Dropdown damageEffectDropdown;

		internal ArtifactData inputData = new ArtifactData();

		private ConsoleState consoleState;

		bool isUpdating;

		private ArtifactConsole console;

		[SerializeField]
		private NetText_label NameLabel = null;
		[SerializeField]
		private NetText_label LogLabel = null;
		[SerializeField]
		private NetText_label OutputLabel = null;

		[SerializeField]
		private NetSpriteImage ImageObject = null;

		public void Awake()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			console = Provider.GetComponent<ArtifactConsole>();

			inputData = console.InputData;

			console.StateChange += UpdateGUI;

			if (!CustomNetworkManager.Instance._isServer) yield break;

			UpdateGUI();

			consoleState = ConsoleState.Idle;

			OnTabOpened.AddListener(UpdateGUIForPeepers);

		}

		public void UpdateGUIForPeepers(PlayerInfo notUsed)
		{
			if (!isUpdating)
			{
				isUpdating = true;
				StartCoroutine(WaitForClient());
			}
		}

		private IEnumerator WaitForClient()
		{
			yield return new WaitForSeconds(0.2f);
			UpdateGUI();
			isUpdating = false;
		}

		public void UpdateGUI()
		{
			inputData = console.InputData;

			if (console.ConnectedArtifact != null) NameLabel.MasterSetValue(console.ConnectedArtifact.ID);
			else NameLabel.MasterSetValue("NULL");

			if(LogLabel.Value == "No disk in console!" && console.dataDisk != null)
			{
				LogLabel.MasterSetValue("Disk inserted!");
				OutputLabel.MasterSetValue("");
			}

			radInput.text = inputData.radiationlevel.ToString();
			bluespaceInput.text = inputData.bluespacesig.ToString();
			bananiumInput.text = inputData.bananiumsig.ToString();

			areaEffectDropdown.value = inputData.AreaEffectValue;
			damageEffectDropdown.value = inputData.DamageEffectValue;
			interactEffectDropdown.value = inputData.InteractEffectValue;

			appearanceDropdown.value = (int)inputData.Type;

			if (console.HasDisk) ImageObject.SetSprite(0);
			else ImageObject.SetSprite(1);

		}

		public void WriteData()
		{
			consoleState = ConsoleState.Writing;

			inputData = console.InputData;

			if (console.dataDisk == null)
			{
				LogLabel.MasterSetValue("No disk in console!");
				OutputLabel.MasterSetValue("Data write unsuccessful.");
				return;
			}
			if (console.ConnectedArtifact == null)
			{
				LogLabel.MasterSetValue("No artifact connected to console!!");
				OutputLabel.MasterSetValue("Data write unsuccessful.");
				return;
			}

			LogLabel.MasterSetValue($"Disk sucessfully found in console" +
				$"\n\nData for artifact {console.ConnectedArtifact.ID} already exists on disk... " +
				$"overriding\n\nWriting data for artifact {console.ConnectedArtifact.ID}");

			OutputLabel.MasterSetValue("Data write successful.");

			foreach (ArtifactDataFile data in console.dataDisk.DataOnStorage)
			{
				data.inputData = inputData;
				data.correctData = console.ConnectedArtifact.artifactData;
			}

			console.dataDisk.CalculateExportCost();

			consoleState = ConsoleState.Idle;

			UpdateGUI();

		}


		public void EjectDisk()
		{
			if (consoleState != ConsoleState.Idle) return;
			if (console.dataDisk != null)
			{
				console.DropDisk();
				LogLabel.MasterSetValue($"Disk ejected from console.");
			}
			else
			{
				LogLabel.MasterSetValue($"No disk in console to eject.");
			}

			OutputLabel.MasterSetValue("Please insert disk.");
			UpdateGUI();
		}

		public void UpdateData()
		{
			Int32.TryParse(radInput.text, out int A);
			inputData.radiationlevel = A;

			Int32.TryParse(bluespaceInput.text, out int B);
			inputData.bluespacesig = B;

			Int32.TryParse(bananiumInput.text, out int C);
			inputData.bananiumsig = C;

			inputData.Type = (ArtifactType)appearanceDropdown.value;

			inputData.AreaEffectValue = areaEffectDropdown.value;
			inputData.DamageEffectValue = damageEffectDropdown.value;
			inputData.InteractEffectValue = interactEffectDropdown.value;

			if(CustomNetworkManager.Instance._isServer == false)
			{
				console.CmdSetInputData(this.inputData);
			}
			else
			{
				console.SetInputDataServer(this.inputData);
			}
		}

		private void OnDestroy()
		{
			console.StateChange -= UpdateGUI;
		}

		public enum ConsoleState
		{
			Writing = 0,
			Idle = 1,
		}
	}
}
