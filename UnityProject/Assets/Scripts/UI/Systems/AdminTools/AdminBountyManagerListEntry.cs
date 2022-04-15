using Systems.Cargo;
using TMPro;
using UnityEngine;

namespace UI.Systems.AdminTools
{
	public class AdminBountyManagerListEntry : MonoBehaviour
	{
		public int BountyIndex;
		public TMP_Text bountyDesc;
		public TMP_Text bountyReward;

		public void Setup(int index, string desc, int reward)
		{
			BountyIndex = index;
			bountyDesc.text = desc;
			bountyReward.text = reward.ToString();
		}

		public void RemoveBounty()
		{
			CargoManager.Instance.CmdRemoveBounty(BountyIndex, false);
			Destroy(gameObject);
		}

		public void CompleteBounty()
		{
			CargoManager.Instance.CmdRemoveBounty(BountyIndex, true);
			Destroy(gameObject);
		}
	}
}