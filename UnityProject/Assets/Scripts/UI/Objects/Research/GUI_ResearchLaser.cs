using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Research;
using System.Text;

namespace UI.Objects.Research
{
	public class GUI_ResearchLaser : NetTab
	{

		[SerializeField] private NetSpriteImage serverConnectionImage;
		[SerializeField] private NetSpriteImage modeToggleImage;
		[SerializeField] private NetText_label technologyProgressLabel;
		[SerializeField] private NetText_label outputLabel;
		[SerializeField] private NetText_label nameLabel;
		[SerializeField] private NetText_label modeLabel;

		private ResearchLaserProjector projector;
		private bool isUpdating = false;

		private int clearInterval = 0;

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

			projector = Provider.GetComponent<ResearchLaserProjector>();

			UpdateGUI();
			Initialise();

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

		private void Initialise()
		{
			projector.UpdateGUI += UpdateGUI;
		}

		public void OnDestroy()
		{
			if(projector != null) projector.UpdateGUI -= UpdateGUI;
		}

		public void ToggleLaserMode()
		{
			if(projector.ProjectorState == LaserProjectorState.Live) projector.UpdateState(LaserProjectorState.Visual);
			else projector.UpdateState(LaserProjectorState.Live);

			UpdateGUI();
		}

		public void ToggleLaserPower()
		{
			if (projector.ProjectorState == LaserProjectorState.Visual)
			{
				if (projector.IsVisualOn == false) projector.TriggerLaser();
				else projector.DisableLaser();
			}
			else if (projector.OnCoolDown == false && projector.ProjectorState == LaserProjectorState.Live)
			{
				projector.FireLaser();
			}

		}

		public void UploadButton()
		{
			projector.TransferDataToRP();
			UpdateGUI();
		}

		public void UpdateGUI()
		{
			UpdateTechnologyList();

			serverConnectionImage.SetSprite(projector.researchServer != null ? 1 : 0);
			modeToggleImage.SetSprite(projector.ProjectorState == LaserProjectorState.Visual ? 0 : 1);

			nameLabel.SetValue(projector.GetComponent<ObjectAttributes>().ArticleName);
			modeLabel.SetValue(projector.ProjectorState.ToString());

			StringBuilder sb = new StringBuilder();
			foreach(string line in projector.OutputLogs)
			{
				sb.AppendLine(line);
			}
			sb.AppendLine(">_");
			outputLabel.SetValue(sb.ToString());

			clearInterval++;
			if (projector.OutputLogs.Count > 0 && clearInterval >= 3)
			{
				projector.OutputLogs.RemoveAt(0); //Gradually clear logs
				clearInterval = 0;
			}
		}

		private void UpdateTechnologyList()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("Current research data:\n");

			foreach(var entry in projector.GroupedData)
			{
				sb.AppendLine($"-{entry.Key.DisplayName} => {entry.Value}/{entry.Key.ResearchCosts}");
			}

			technologyProgressLabel.SetValue(sb.ToString());
		}

		
	}
}
