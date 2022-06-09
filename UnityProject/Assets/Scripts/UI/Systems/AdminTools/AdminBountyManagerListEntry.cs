using AdminCommands;
using TMPro;
using UnityEngine;

namespace UI.Systems.AdminTools
{
	public class AdminBountyManagerListEntry : MonoBehaviour
	{
		public int BountyIndex;
		public TMP_Text bountyDesc;
		public TMP_InputField bountyReward;

		public void Setup(int index, string desc, int reward)
		{
			BountyIndex = index;
			bountyDesc.text = desc;
			bountyReward.text = reward.ToString();
		}

		private void Update()
		{
			//(Max) : Keycode return is ENTER. No idea why it's called like this.
			if(Input.GetKeyDown(KeyCode.Return) == false) return;
			var newReward = int.Parse(bountyReward.text);
			if(newReward < 0) return;
			AdminCommandsManager.Instance.CmdAdjustBountyRewards(BountyIndex, newReward);
		}

		public void RemoveBounty()
		{
			AdminCommandsManager.Instance.CmdRemoveBounty(BountyIndex, false);
			AdminCommandsManager.Instance.CmdRequestCargoServerData();
		}

		public void CompleteBounty()
		{
			AdminCommandsManager.Instance.CmdRemoveBounty(BountyIndex, true);
			AdminCommandsManager.Instance.CmdRequestCargoServerData();
		}
	}
}