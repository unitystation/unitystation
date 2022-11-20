using UI.Core.NetUI;
using UnityEngine;
using Systems.Research.Objects;
using Systems.Research;
using System.Collections.Generic;
using System.Linq;
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

		private BlastYieldDetector _blastYieldDetector;

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
		private GUI_BYDGraph blastYieldGraph;

		#endregion

		#region Initialization
		private void Start()
		{
			BlastYieldDetector.blastEvent += OnRecieveBlast;
			BlastYieldDetector.updateGUIEvent += UpdateGui;

			UpdateGui();

			blastYieldGraph.UpdateDataDisplay(blastYieldDetector);
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

			blastYieldGraph.UpdateDataDisplay(blastYieldDetector);

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

			bountyList = blastYieldDetector.researchServer?.ExplosiveBounties.ToList(); //Clears current bounty list and updates to match current bounty list on research server.
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
		public void SetCurrentShownData(int pos)
		{
			blastYieldGraph.UpdateDataDisplay(blastYieldDetector);
		}

		public void OnDestroy()
		{
			BlastYieldDetector.blastEvent -= OnRecieveBlast;
			BlastYieldDetector.updateGUIEvent -= UpdateGui;
		}

		
	}
}