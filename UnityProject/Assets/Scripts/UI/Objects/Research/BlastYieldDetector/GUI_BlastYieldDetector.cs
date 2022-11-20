using System;
using UI.Core.NetUI;
using UnityEngine;
using Systems.Research.Objects;
using Systems.Research;
using System.Collections.Generic;
using Chemistry;
using Items.Weapons;

namespace UI.Objects.Research
{
	public class GUI_BlastYieldDetector : NetTab
	{
		public BlastYieldDetector blastYieldDetector
		{
			get
			{
				if (!_blastYieldDetector)
				{
					_blastYieldDetector = Provider.GetComponentInChildren<BlastYieldDetector>();
				}

				return _blastYieldDetector;
			}
		}

		private const int YAXIS_MAX = 1000; //Up to 1000 Blast yield
		private const int XAXIS_MAX = 10; //Up to last 10 datapoints

		private BlastYieldDetector _blastYieldDetector;

		private GUI_BlastYieldDetector clientGUI;
		private Transform clientGUIGraphTransform;

		private List<ExplosiveBounty> bountyList;

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

		/// <summary>
		/// Offset to position highlight line UI properly
		/// </summary>
		public float rectOffset;
		[SerializeField]
		private RectTransform yieldNodeHighlight;

		[SerializeField]
		private RectTransform pointNodeHighlight;

		#endregion

		#region Initialization
		private void Start()
		{
			clientGUI = UIManager.Instance.transform.GetChild(0).GetComponentInChildren<GUI_BlastYieldDetector>();
			clientGUIGraphTransform = clientGUI.graphContainer.transform;
			BlastYieldDetector.blastEvent += OnRecieveBlast;
			BlastYieldDetector.updateGUIEvent += UpdateGui;
			UpdateDataDisplay();
			UpdateGui();
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
			if (blastYieldDetector == null) return;

			if(blastYieldDetector.researchServer == null)
			{
				bountyContainer.Clear();
				bountyList.Clear();
			}
			else
			{
				pointsLabel.MasterSetValue(blastYieldDetector.researchServer.RP.ToString());
			}

			bountyList = blastYieldDetector.researchServer?.ExplosiveBounties; //Clears current bounty list and updates to match current bounty list on research server.
			UpdateBountyContainerToList(bountyList);
		
		}

		private void UpdateBountyContainerToList(List<ExplosiveBounty> bounties)
		{
			bountyContainer.Clear();
			if (bounties == null) return;

			bountyContainer.SetItems(bounties.Count);

			for (int i = 0; i < bounties.Count; i++)
			{
				var entryScript = bountyContainer.Entries[i].GetComponent<ExplosiveBountyUIEntry>();
				entryScript.Initialise(bounties[i], i);
			}
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

		public void SetCurrentShownData(int pos)
		{
			UpdateDataDisplay();
		}

		/// <summary>
		/// Moves highlight lines to current node, and updates labels
		/// </summary>
		private void UpdateDataDisplay()
		{
			if (blastYieldDetector == null || blastYieldDetector.BlastYieldData == null)
			{
				graphContainer.Clear();
				return;
			}

			List<float> yields = blastYieldDetector.BlastYieldData;
			if (yields.Count > XAXIS_MAX)
			{
				yields = yields.GetRange(blastYieldDetector.BlastYieldData.Count - 1 - XAXIS_MAX, XAXIS_MAX); //Obtains last ten datapoints
			}

			graphContainer.SetItems(yields.Count);

			for (int i = 0; i < yields.Count; i++)
			{
				Vector2 dataShownPos = GetNodePosition(yields[i], i);

				if (i < clientGUIGraphTransform.childCount)
				{
					clientGUIGraphTransform.GetChild(i).GetComponent<RectTransform>().anchoredPosition = dataShownPos;
				}

				if (i != yields.Count - 1) continue;

				Vector3 yieldNewY = yieldNodeHighlight.anchoredPosition;
				yieldNewY.y = dataShownPos.y + rectOffset;
				clientGUI.yieldNodeHighlight.anchoredPosition = yieldNewY;

				Vector3 pointNewX = pointNodeHighlight.anchoredPosition;
				pointNewX.x = dataShownPos.x + rectOffset;
				clientGUI.pointNodeHighlight.anchoredPosition = pointNewX;
			}
		}

		#endregion
	}
}