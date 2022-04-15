using Systems.Cargo;
using TMPro;
using UnityEngine;

namespace UI.Systems.AdminTools
{
	public class AdminBountyManagerListEntry : MonoBehaviour
	{
		public CargoBounty bounty;
		public TMP_Text bountyDesc;
		public TMP_Text bountyReward;

		public void Setup(CargoBounty b)
		{
			bounty = b;
			bountyDesc.text = b.Description;
			bountyReward.text = b.Reward.ToString();
		}

		public void RemoveBounty()
		{
			CargoManager.Instance.ActiveBounties.Remove(bounty);
			Destroy(gameObject);
		}

		public void CompleteBounty()
		{
			CargoManager.Instance.CompleteBounty(bounty);
			Destroy(gameObject);
		}
	}
}