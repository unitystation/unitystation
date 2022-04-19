using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Research.Objects;

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
		private NetLabel rpLabel = null;
		[SerializeField]
		private NetSpriteImage connection = null;

		[SerializeField]
		private NetLabel scanStatusLabel = null;

		//Results
		[SerializeField]
		private NetLabel radLabel = null;
		[SerializeField]
		private NetLabel bluespaceLabel = null;
		[SerializeField]
		private NetLabel clownLabel = null;
		[SerializeField]
		private NetLabel massLabel = null;
		[SerializeField]
		private NetLabel completeLabel = null;

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

			ArtifactAnalyser.stateChange += UpdateGUI;

			OnTabOpened.AddListener(UpdateGUIForPeepers);

			Logger.Log(nameof(WaitForProvider), Category.Research);
		}

		public void UpdateGUIForPeepers(ConnectedPlayer notUsed)
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
			rpLabel.SetValueServer("Stored RP: " + artifactAnalyser.storedRP);
		}

		public void UpdateGUI()
		{
			UpdateServerConnectionImage();
			UpdateRPDisplay();

			if (artifactAnalyser.itemStorage.GetIndexedItemSlot(0).IsOccupied)
			{
				artifactSliver = artifactAnalyser.artifactSliver;
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
				completeLabel.SetValueServer("Analysis in Progress.");
				yield return new WaitForSeconds(1f);
				completeLabel.SetValueServer("Analysis in Progress");
				yield return new WaitForSeconds(1f);
			}
			artifactAnalyser.SetState(AnalyserState.Idle);

			int rp = 0;

			if (!artifactAnalyser.researchServer.researchedSlivers.Contains(artifactSliver.ID))
			{
				rp = artifactSliver.RPReward; //Only give us RP if this sliver has not previously been researched for this server.
				artifactAnalyser.researchServer.researchedSlivers.Add(artifactSliver.ID);
			}

			completeLabel.SetValueServer("Analysis Complete! +" + rp + "RP!");
			artifactAnalyser.storedRP += rp;
			UpdateRPDisplay();

			radLabel.SetValueServer("Radiataion Level:\n" + artifactSliver.radiationlevel + "rad");
			bluespaceLabel.SetValueServer("Bluespace Signature:\n" + artifactSliver.bluespacesig + "Gy");
			clownLabel.SetValueServer("Bananium Signature:\n" + artifactSliver.bananiumsig + "mClw");
			massLabel.SetValueServer("Sample mass:\n" + artifactSliver.mass + "g");
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
				completeLabel.SetValueServer("Deconstructing Sample.");
				yield return new WaitForSeconds(1f);
				completeLabel.SetValueServer("Deconstructing Sample");
				yield return new WaitForSeconds(1f);
			}
			artifactAnalyser.SetState(AnalyserState.Idle);

			completeLabel.SetValueServer("Deconstruction Successful!");

			artifactAnalyser.DestroySample();

		}

		public void UploadToServer()
		{
			if(artifactAnalyser.researchServer == null) return;

			if(artifactAnalyser.researchServer.AddPointsToTechWeb(artifactAnalyser.storedRP))
			{
				artifactAnalyser.storedRP = 0;
				UpdateRPDisplay();
			}
			return;
		}

		public void EjectSample()
		{
			if (artifactAnalyser.analyserState != AnalyserState.Idle) return;
			artifactAnalyser.EjectSample();
			SwitchToNullPage();
		}

		public void RemoveId(ConnectedPlayer player)
		{
			CloseTab();
		}

		public void SwitchToNullPage()
		{
			radLabel.SetValueServer("Radiataion Level:\nnull");
			bluespaceLabel.SetValueServer("Bluespace Signature:\nnull");
			clownLabel.SetValueServer("Bananium Signature:\nnull");
			massLabel.SetValueServer("Sample mass:\nnull");
			completeLabel.SetValueServer("No Analysis Performed.");

			mainSwitcher.SetActivePage(0);

		}
		private void OnDestroy()
		{
			ArtifactAnalyser.stateChange -= UpdateGUI;
		}
	}
}
