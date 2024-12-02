using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using System;
using Objects.Research;
using UnityEngine.UI;
using System.Text;
using Items.Others;
using Systems.Cargo;


namespace UI.Objects.Research
{
	public class GUI_ArtifactConsole : NetTab
	{
		[SerializeField]
		private GameObject anomalyReportPrefab = null;

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

		bool isUpdating;

		private ArtifactConsole console;

		[SerializeField]
		private NetText_label NameLabel = null;

		[SerializeField]
		private NetText_label OutputLabel = null;

		[SerializeField]
		private NetColorChanger DormantPic = null;
		[SerializeField]
		private NetColorChanger ActivePic = null;


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

			UpdateArtifactInfo();

			radInput.text = inputData.radiationlevel.ToString();
			bluespaceInput.text = inputData.bluespacesig.ToString();
			bananiumInput.text = inputData.bananiumsig.ToString();

			areaEffectDropdown.SetValueWithoutNotify(inputData.AreaEffectValue);
			damageEffectDropdown.SetValueWithoutNotify(inputData.DamageEffectValue);
			interactEffectDropdown.SetValueWithoutNotify(inputData.InteractEffectValue);

			appearanceDropdown.SetValueWithoutNotify((int)inputData.Type);

		}

		public void PrintReport(PlayerInfo playerInfo)
		{
			inputData = console.InputData;

			if (console.ConnectedArtifact == null)
			{
				OutputLabel.MasterSetValue("Data write unsuccessful.");
				return;
			}

			OutputLabel.MasterSetValue("Data write successful.\nPrinting Report...");

			var p = Spawn.ServerPrefab(anomalyReportPrefab, console.gameObject.RegisterTile().WorldPositionServer, console.transform.parent).GameObject;
			var paper = p.GetComponent<Paper>();
			paper.SetServerString(GenerateReportText(inputData, playerInfo.Name));

			UpdateGUI();

		}

		public void EraseData(PlayerInfo playerInfo)
		{
			OutputLabel.MasterSetValue($"{playerInfo.Name} erased console data.");

			ArtifactData blankData = new ArtifactData();

			console.SetInputDataServer(blankData);
			console.UnSubscribeFromServerEvent();

			UpdateArtifactInfo();
		}

		private void UpdateArtifactInfo()
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (console.ConnectedArtifact != null)
			{
				inputData.ID = console.ConnectedArtifact.ID;
				console.InputData.ID = console.ConnectedArtifact.ID;

				NameLabel.MasterSetValue(inputData.ID);
				if (console.ConnectedArtifact.isDormant)
				{
					DormantPic.MasterSetValue(Color.white);
					ActivePic.MasterSetValue(Color.gray);
				}
				else
				{
					DormantPic.MasterSetValue(Color.gray);
					ActivePic.MasterSetValue(Color.white);
				}
			}
			else
			{
				NameLabel.MasterSetValue("NULL");
				DormantPic.MasterSetValue(Color.gray);
				ActivePic.MasterSetValue(Color.gray);
			}
		}

		public string GenerateReportText(ArtifactData data, string signee)
		{
			
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"<size=30><b>=={CargoManager.ANOMALY_REPORT_TITLE_STRING.ToUpper()}==</b></size>");
			sb.AppendLine($"\nReport conducted by {signee}");

			sb.AppendLine($"\n[AIN] Anomaly Identification: {data.ID}");

			sb.AppendLine($"\n[APP] Appearance: {data.Type}");

			sb.AppendLine($"\n[RAL] Radiation Level: {data.radiationlevel}rad");
			sb.AppendLine($"[BSA] Bluespace Signature: {data.bluespacesig}Gy");
			sb.AppendLine($"[BSB] Bananium Signature: {data.bananiumsig}mClw");

			sb.AppendLine($"\n[PIF] Passive Influence: {CargoManager.areaNames[data.AreaEffectValue]}");
			sb.AppendLine($"[ONT] On-Interaction: {CargoManager.interactNames[data.InteractEffectValue]}");
			sb.AppendLine($"[OIL] On-Integrity Loss: {CargoManager.damageNames[data.DamageEffectValue]}");

			sb.AppendLine($"\n<size=30><b>==END OF ANOMALY REPORT==</b></size>");

			return sb.ToString();
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
