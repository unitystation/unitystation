using UI.Core.NetUI;
using UnityEngine;
using System;
using Systems.Research.Objects;
using Systems.Research;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Chemistry;
using Items.Weapons;

namespace UI.Objects.Research
{
	public class GUI_BlastYieldDetector : NetTab
	{
		private BlastYieldDetector blastYieldDetector;

		private bool isUpdating;

		private const int YAXIS_MAX = 1000; //Up to 1000 Blast yield
		private const int XAXIS_MAX = 10; //Up to last 10 datapoints

		/// <summary>
		/// Offset to position highlight line UI properly
		/// </summary>
		public float rectOffset;

		#region Serializefields

		[SerializeField]
		private NetText_label smokeLabel;
		[SerializeField]
		private Reaction smokeReaction;

		[SerializeField]
		private NetText_label foamLabel;
		[SerializeField]
		private Reaction foamReaction;

		[SerializeField]
		private NetText_label yieldLabel;

		[SerializeField]
		private NetText_label reagentLabel;

		[SerializeField]
		private NetText_label pointsLabel;

		[SerializeField]
		private EmptyItemList bountyContainer;

		[SerializeField]
		public EmptyItemList graphContainer;

		[SerializeField]
		private NetAnchoredPosition horizontalNodeHighlight;

		[SerializeField]
		private NetAnchoredPosition verticalNodeHighlight;

		#endregion

		#region Initialization

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

			blastYieldDetector = Provider.GetComponent<BlastYieldDetector>();

			BlastYieldDetector.blastEvent += OnRecieveBlast;
			BlastYieldDetector.updateGUIEvent += UpdateGui;

			UpdateGui();
			UpdateDataDisplay();

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
			UpdateGui();
			isUpdating = false;
		}


		#endregion

		private void OnRecieveBlast(BlastData data)
		{  
			var mix = data.ReagentMix;
			if (smokeReaction.IsReactionValid(mix)) smokeLabel.MasterSetValue(smokeReaction.GetReactionAmount(mix).ToString());
			else smokeLabel.MasterSetValue("0");

			if (foamReaction.IsReactionValid(mix)) foamLabel.MasterSetValue(foamReaction.GetReactionAmount(mix).ToString());
			else foamLabel.MasterSetValue("0");

			reagentLabel.MasterSetValue(data.ReagentMix.Total.ToString());
			yieldLabel.MasterSetValue(blastYieldDetector.BlastYieldData[blastYieldDetector.BlastYieldData.Count - 1].ToString());

			UpdateDataDisplay();

			UpdateGui();
		}

		public void UpdateGui()
		{
			if(blastYieldDetector.researchServer != null) pointsLabel.MasterSetValue(blastYieldDetector.researchServer.RP.ToString());
			
			UpdateBountyContainerToList();	
		}

		private void UpdateBountyContainerToList()
		{
			bountyContainer.Clear();

			List<ExplosiveBounty> bounties = blastYieldDetector.researchServer?.ExplosiveBounties.ToList();

			if (bounties == null) return;

			bountyContainer.SetItems(bounties.Count);

			for (int i = 0; i < bounties.Count; i++)
			{
				var entryScript = bountyContainer.Entries[i].GetComponent<ExplosiveBountyUIEntry>();
				entryScript.Initialise(bounties[i], i);
			}
		}
		public void SetCurrentShownData(int pos)
		{
			UpdateDataDisplay();
		}

		public void OnDestroy()
		{
			BlastYieldDetector.blastEvent -= OnRecieveBlast;
			BlastYieldDetector.updateGUIEvent -= UpdateGui;
		}

		#region Plotting

		public Vector2 GetNodePosition(float yield, float index)
		{
			float yieldClamp = Math.Min(yield, YAXIS_MAX);

			float dotPosY = yieldClamp * graphContainer.GetComponent<RectTransform>().rect.height / YAXIS_MAX;

			//points axis position
			float dotPosX = index * graphContainer.GetComponent<RectTransform>().rect.width / XAXIS_MAX;

			//position 2d, third axis isn't important
			Vector2 dotPosition = new Vector2(dotPosX, dotPosY);
			return dotPosition;
		}

		private void UpdateDataDisplay()
		{
			if (blastYieldDetector == null || blastYieldDetector.BlastYieldData == null)
			{
				graphContainer.Clear();
				return;
			}

			List<float> yields = blastYieldDetector.BlastYieldData.ToList();
			if (yields.Count > XAXIS_MAX)
			{
				yields = yields.GetRange(blastYieldDetector.BlastYieldData.Count - 1 - XAXIS_MAX, XAXIS_MAX); //Obtains last ten datapoints
			}

			graphContainer.SetItems(yields.Count);

			for (int i = 0; i < yields.Count; i++)
			{
				Vector2 dataShownPos = GetNodePosition(yields[i], i);

				graphContainer.Entries[i].GetComponentInChildren<NetAnchoredPosition>().SetPosition(dataShownPos);
				
				if (i != yields.Count - 1) continue;

				Vector3 yieldNewY = horizontalNodeHighlight.Element.anchoredPosition;
				yieldNewY.y = dataShownPos.y + rectOffset;
				horizontalNodeHighlight.SetPosition(yieldNewY);

				Vector3 indexNewX = verticalNodeHighlight.Element.anchoredPosition;
				indexNewX.x = dataShownPos.x + rectOffset;
				verticalNodeHighlight.SetPosition(indexNewX);
			}
		}
	
		#endregion
}
}