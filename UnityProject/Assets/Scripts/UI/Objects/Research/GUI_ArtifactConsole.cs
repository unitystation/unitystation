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
		private Dropdown contactEffectDropdown;
		[SerializeField]
		private Dropdown areaEffectDropdown;
		[SerializeField]
		private Dropdown damageEffectDropdown;
		[SerializeField]
		private Dropdown solidEffectDropdown;
		[SerializeField]
		private Dropdown gasEffectDropdown;

		private ArtifactData inputData = new ArtifactData();

		private ConsoleState consoleState;

		bool isUpdating;

		private ArtifactConsole console;
		public ArtifactConsoleDataSO consoleData;

		public NetLabel NameLabel = null;
		public NetLabel LogLabel = null;
		public NetLabel OutputLabel = null;

		public NetSpriteImage ImageObject = null;

		protected override void InitServer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				StartCoroutine(WaitForProvider());
			}
		}

		private IEnumerator WaitForProvider()
		{

			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			console = Provider.GetComponent<ArtifactConsole>();
			inputData = console.inputData;

			UpdateGUI();

			consoleState = ConsoleState.Idle;

			ArtifactConsole.stateChange += UpdateGUI;

			OnTabOpened.AddListener(UpdateGUIForPeepers);

			Logger.Log(nameof(WaitForProvider), Category.Research);
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

			bool hidden = console.dataDisk == null;

			if (hidden) ImageObject.SetSprite(1);
			else ImageObject.SetSprite(0);

			if(console.connectedArtifact != null) NameLabel.SetValueServer(console.connectedArtifact.ID);
			else NameLabel.SetValueServer("NULL");

			if(LogLabel.Value == "No disk in console!" && console.dataDisk != null)
			{
				LogLabel.SetValueServer("Disk inserted!");
				OutputLabel.SetValueServer("");
			}

			radInput.text = inputData.radiationlevel.ToString();
			bluespaceInput.text = inputData.bluespacesig.ToString();
			bananiumInput.text = inputData.bananiumsig.ToString();

			areaEffectDropdown.value = console.AreaDropDownChoice;
			contactEffectDropdown.value = console.ContactDropDownChoice;
			damageEffectDropdown.value = console.DamageDropDownChoice;
			solidEffectDropdown.value = console.SolidDropDownChoice;
			gasEffectDropdown.value = console.GasDropDownChoice;

			appearanceDropdown.value = (int)inputData.Type;
			inputData = console.inputData;
		}

		public void WriteData()
		{
			consoleState = ConsoleState.Writing;

			inputData = console.inputData;

			inputData.AreaEffect = consoleData.AreaEffects[areaEffectDropdown.value];
			inputData.FeedEffect = consoleData.FeedEffects[solidEffectDropdown.value];
			inputData.ContactEffect = consoleData.ContactEffects[contactEffectDropdown.value];
			inputData.GasEffect = consoleData.GasReactEffects[gasEffectDropdown.value];
			inputData.DamageEffect = consoleData.DamageEffects[damageEffectDropdown.value];

			console.inputData = inputData;

			if (console.dataDisk == null)
			{
				LogLabel.SetValueServer("No disk in console!");
				OutputLabel.SetValueServer("Data write unsuccessful.");
				return;
			}
			if (console.connectedArtifact == null)
			{
				LogLabel.SetValueServer("No artifact connected to console!!");
				OutputLabel.SetValueServer("Data write unsuccessful.");
				return;
			}
			 
			LogLabel.SetValueServer($"Disk sucessfully found in console" +
				$"\n\nData for artifact {console.connectedArtifact.ID} already exists on disk... " +
				$"overriding\n\nWriting data for artifact {console.connectedArtifact.ID}");

			OutputLabel.SetValueServer("Data write successful.");

			foreach (ArtifactDataFiles data in console.dataDisk.DataOnStorage)
			{
				data.inputData = inputData;
				data.correctData = console.connectedArtifact.artifactData;
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
				LogLabel.SetValueServer($"Disk ejected from console.");
			}
			else
			{
				LogLabel.SetValueServer($"No disk in console to eject.");
			}

			OutputLabel.SetValueServer("Please insert disk.");
			UpdateGUI();
		}

		public void UpdateData()
		{
			console = Provider.GetComponent<ArtifactConsole>();

			Int32.TryParse(radInput.text, out int A);
			inputData.radiationlevel = A;

			Int32.TryParse(bluespaceInput.text, out int B);
			inputData.bluespacesig = B;

			Int32.TryParse(bananiumInput.text, out int C);
			inputData.bananiumsig = C;

			inputData.Type = (ArtifactType)appearanceDropdown.value;

			console.AreaDropDownChoice = areaEffectDropdown.value;
			console.ContactDropDownChoice = contactEffectDropdown.value;
			console.DamageDropDownChoice = damageEffectDropdown.value;
			console.SolidDropDownChoice = solidEffectDropdown.value;
			console.GasDropDownChoice = gasEffectDropdown.value;

			console.inputData = inputData;

			UpdateGUI();
		}

		private void OnDestroy()
		{
			ArtifactConsole.stateChange -= UpdateGUI;
		}

		public enum ConsoleState
		{
			Writing = 0,
			Idle = 1,
		}
	}
}
