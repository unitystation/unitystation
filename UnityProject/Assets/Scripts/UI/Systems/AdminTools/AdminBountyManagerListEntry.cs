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
			if(Input.GetKey(KeyCode.KeypadEnter) == false || Input.GetKey(KeyCode.Return) == false) return;
			if(bountyReward.isFocused == false) return;
			var newReward = int.Parse(bountyReward.text);
			if(newReward < 0) return;
			AdminCommandsManager.Instance.CmdAdjustBountyRewards(BountyIndex, int.Parse(bountyReward.text));
		}

		public void RemoveBounty()
		{
			AdminCommandsManager.Instance.CmdRemoveBounty(BountyIndex, false);
			Destroy(gameObject);
		}

		public void CompleteBounty()
		{
			AdminCommandsManager.Instance.CmdRemoveBounty(BountyIndex, true);
			Destroy(gameObject);
		}
	}
}