using Logs;
using Mirror;

namespace Systems.Faith
{
	public class PlayerFaith : NetworkBehaviour, IRightClickable
	{
		public PlayerScript player;
		private Faith currentFaith = null;

		public Faith CurrentFaith
		{
			get => currentFaith;
			private set => currentFaith = value;
		}

		[field: SyncVar] public string FaithName { get; private set; } = "None";

		[Server]
		public void JoinReligion(Faith newFaith)
		{
			if (newFaith == null)
			{
				Loggy.LogError("[PlayerFaith] - Cannot join a null faith.");
				return;
			}
			currentFaith = newFaith;
			FaithName = currentFaith.FaithName;
			FaithManager.JoinFaith(newFaith, player);
		}

		[Command]
		public void JoinReligion(string newFaith)
		{
			JoinReligion(FaithManager.Instance.AllFaiths.Find(x => x.Faith.FaithName == newFaith).Faith);
		}

		[Command]
		public void LeaveReligion()
		{
			currentFaith = null;
			FaithName = "None";
			FaithManager.LeaveFaith(player);
		}

		[Command]
		public void AddNewFaithLeader()
		{
			if (currentFaith == null) return;
			FaithManager.AddLeaderToFaith(CurrentFaith.FaithName, player);
		}

		[Command]
		public void CreateNewFaith(string selectedFaith)
		{
			FaithManager.Instance.AddFaithToActiveList(FaithManager.Instance.AllFaiths
				.Find(x => x.Faith.FaithName == selectedFaith).Faith);
		}

		[TargetRpc]
		public void RpcShowFaithSelectScreen(NetworkConnection target)
		{
			UIManager.Instance.ChaplainFirstTimeSelectScreen.gameObject.SetActive(true);
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			RightClickableResult result = new RightClickableResult();
			if (FaithName != "None")
			{
				result.AddElement("Join Faith",
					() => PlayerManager.LocalPlayerScript.PlayerFaith.JoinReligion(FaithName));
			}

			return result;
		}

		public string ToleranceCheckForReligion()
		{
			//This is client trickery, anything we want to check on the client itself is from PlayerManager
			//while things on the other player is done directly from within this class
			if (PlayerManager.LocalPlayerScript.PlayerFaith.currentFaith == null) return "";
			string finalText = "";
			if (FaithName == "None")
			{
				finalText = "This person does not appear to be a part of any faith.";
			}
			else
			{
				switch (PlayerManager.LocalPlayerScript.PlayerFaith.currentFaith.ToleranceToOtherFaiths)
				{
					case ToleranceToOtherFaiths.Accepting:
						finalText = "";
						break;
					case ToleranceToOtherFaiths.Neutral:
						if (PlayerManager.LocalPlayerScript.PlayerFaith.FaithName != FaithName)
						{
							finalText = $"This person appears to have faith in {FaithName}.";
						}
						else
						{
							finalText = $"<color=green>This person appears to share the same faith as me!</color>";
						}

						break;
					case ToleranceToOtherFaiths.Rejecting:
						if (PlayerManager.LocalPlayerScript.PlayerFaith.FaithName != FaithName)
						{
							finalText =
								$"<color=red>This person appears to have faith in {FaithName} which goes against what I believe.</color>";
						}
						else
						{
							finalText = $"<color=green>This person appears to share the same faith as me!</color>";
						}

						break;
					case ToleranceToOtherFaiths.Violent:
						if (PlayerManager.LocalPlayerScript.PlayerFaith.FaithName != FaithName)
						{
							finalText =
								$"<color=red>This person appears to not share the same beliefs as me, and I don't like that.</color>";
						}
						else
						{
							finalText = $"<color=green>This person appears to share the same faith as me!</color>";
						}

						break;
					default:
						finalText = "";
						break;
				}
			}
			return finalText;
		}
	}
}