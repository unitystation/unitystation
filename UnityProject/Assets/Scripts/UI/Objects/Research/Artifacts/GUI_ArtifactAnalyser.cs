using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Research.Objects;
using Items.Science;

namespace UI.Objects.Research
{
	public class GUI_ArtifactAnalyser : NetTab
	{
		[SerializeField]
		private NetPageSwitcher mainSwitcher = null;
		[SerializeField]
		private NetPage scanPage = null;
		[SerializeField]
		private NetPage noSamplePage = null;

		[SerializeField]
		private NetText_label rpLabel = null;
		[SerializeField]
		private NetSpriteImage connection = null;

		[SerializeField]
		private NetText_label scanStatusLabel = null;

		//Results
		[SerializeField]
		private NetText_label radLabel = null;
		[SerializeField]
		private NetText_label bluespaceLabel = null;
		[SerializeField]
		private NetText_label clownLabel = null;
		[SerializeField]
		private NetText_label massLabel = null;
		[SerializeField]
		private NetText_label completeLabel = null;

		public ArtifactAnalyser artifactAnalyser;

		private ArtifactSliver artifactSliver;

		bool isUpdating;


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

			artifactAnalyser = Provider.GetComponentInChildren<ArtifactAnalyser>();

			UpdateGUI();

			artifactAnalyser.StateChange += UpdateGUI;

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

		private void UpdateServerConnectionImage()
		{
			if (artifactAnalyser.researchServer == null)
			{
				connection.SetSprite(0);
			}
			else
			{
				connection.SetSprite(1);
			}
		}

		private void UpdateRPDisplay()
		{
			rpLabel.MasterSetValue("Stored RP: " + artifactAnalyser.storedRP);
		}

		public void UpdateGUI()
		{
			UpdateServerConnectionImage();
			UpdateRPDisplay();

			if (artifactAnalyser.itemStorage.GetIndexedItemSlot(0).IsOccupied)
			{
				artifactSliver = artifactAnalyser.ArtifactSliver;
			}
			if (mainSwitcher.CurrentPage == noSamplePage && artifactAnalyser.itemStorage.GetIndexedItemSlot(0).IsOccupied)
			{
				mainSwitcher.SetActivePage(1);
			}
			if(mainSwitcher.CurrentPage == scanPage && artifactAnalyser.itemStorage.GetIndexedItemSlot(0).IsEmpty)
			{
				SwitchToNullPage();
			}

		}

		public void BeginScan()
		{
			if (artifactAnalyser.analyserState != AnalyserState.Idle) return;

			if (artifactSliver != null)
			{
				StartCoroutine(ScanCoroutine());
			}
			else
			{
				Debug.LogWarning("No Artifact Sliver in machine!");
			}
		}

		private IEnumerator ScanCoroutine()
		{
			artifactAnalyser.SetState(AnalyserState.Scanning);

			for(int i = 0; i < 3; i++)
			{
				completeLabel.MasterSetValue("Analysis in Progress.");
				yield return new WaitForSeconds(1f);
				completeLabel.MasterSetValue("Analysis in Progress");
				yield return new WaitForSeconds(1f);
			}
			artifactAnalyser.SetState(AnalyserState.Idle);

			int rp = 0;

			if (artifactAnalyser.researchServer != null)
			{
				if (artifactAnalyser.researchServer.AddArtifactIDtoTechWeb(artifactSliver.ID)) //Give us an RP reward if this sliver is unique to the server
					rp = artifactSliver.RPReward;
			}

			completeLabel.MasterSetValue("Analysis Complete! +" + rp + "RP!");
			artifactAnalyser.storedRP += rp;
			UpdateRPDisplay();

			radLabel.MasterSetValue("Radiation Level:\n" + artifactSliver.sliverData.radiationlevel + "rad");
			bluespaceLabel.MasterSetValue("Bluespace Signature:\n" + artifactSliver.sliverData.bluespacesig + "Gy");
			clownLabel.MasterSetValue("Bananium Signature:\n" + artifactSliver.sliverData.bananiumsig + "mClw");
			massLabel.MasterSetValue("Sample mass:\n" + artifactSliver.sliverData.mass + "g");
		}

		public void DeconstructArtifact()
		{
			if (artifactAnalyser.analyserState != AnalyserState.Idle) return;

			if (artifactSliver == null) return;

			artifactAnalyser.analyserState = AnalyserState.Destroying;

			StartCoroutine(DestroyCoroutine());
		}

		private IEnumerator DestroyCoroutine()
		{
			for (int i = 0; i < 3; i++)
			{
				completeLabel.MasterSetValue("Deconstructing Sample.");
				yield return new WaitForSeconds(1f);
				completeLabel.MasterSetValue("Deconstructing Sample");
				yield return new WaitForSeconds(1f);
			}
			artifactAnalyser.SetState(AnalyserState.Idle);

			completeLabel.MasterSetValue("Deconstruction Successful!");

			artifactAnalyser.DestroySample();

		}

		public void UploadToServer()
		{
			if(artifactAnalyser.researchServer == null) return;

			artifactAnalyser.AddResearchPoints(artifactAnalyser.storedRP);
			artifactAnalyser.storedRP = 0;
			UpdateRPDisplay();
		}

		public void EjectSample()
		{
			if (artifactAnalyser.analyserState != AnalyserState.Idle) return;
			artifactAnalyser.EjectSample();
			SwitchToNullPage();
		}

		public void RemoveId(PlayerInfo player)
		{
			CloseTab();
		}

		public void SwitchToNullPage()
		{
			radLabel.MasterSetValue("Radiation Level:\nnull");
			bluespaceLabel.MasterSetValue("Bluespace Signature:\nnull");
			clownLabel.MasterSetValue("Bananium Signature:\nnull");
			massLabel.MasterSetValue("Sample mass:\nnull");
			completeLabel.MasterSetValue("No Analysis Performed.");

			mainSwitcher.SetActivePage(0);

		}
		private void OnDestroy()
		{
			artifactAnalyser.StateChange -= UpdateGUI;
		}
	}
}
