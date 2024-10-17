using System.Text;
using Logs;
using Mirror;
using Systems.Faith.UI;
using UI.Core.Action;
using UnityEngine;

namespace Systems.Faith
{
	public class PlayerFaith : NetworkBehaviour, IRightClickable, IGameActionHolderSingle
	{
		public PlayerScript player;
		private Faith currentFaith = null;

		public Faith CurrentFaith
		{
			get => currentFaith;
			private set => currentFaith = value;
		}

		[field: SyncVar] public string FaithName { get; private set; } = "None";
		[SerializeField] private ActionData ability;
		public ActionData ActionData => ability;

		public void CallActionClient()
		{
			UIManager.Instance.FaithInfo.gameObject.SetActive(true);
		}

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
			Chat.AddExamineMsg(gameObject, $"You've joined the {FaithName} faith.");
		}

		[Command]
		public void JoinReligion(string newFaith)
		{
			JoinReligion(FaithManager.Instance.AllFaiths.Find(x => x.Faith.FaithName == newFaith).Faith);
			UIActionManager.ToggleServer(gameObject, this, true);
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

		[Command]
		public void CmdUpdateInfoScreenData()
		{
			StringBuilder names = new StringBuilder();
			foreach (var nameOfMember in FaithManager.GetAllMembersOfFaith(FaithName))
			{
				names.AppendLine(nameOfMember.playerName);
			}

			var data = new FaithInfoUI.FaithUIInfo()
			{
				FaithName = FaithName,
				Members = names.ToString(),
				Points = FaithManager.GetPointsOfFaith(FaithName).ToString(),
			};
			RpcUpdateInfoScreenDataForPlayer(player.connectionToClient, data);
		}

		[TargetRpc]
		public void RpcUpdateInfoScreenDataForPlayer(NetworkConnection target, FaithInfoUI.FaithUIInfo data)
		{
			UIManager.Instance.FaithInfo.UpdateData(data);
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			RightClickableResult result = new RightClickableResult();
			if (FaithName != "None")
			{
				result.AddElement("Join Faith",
					() => PlayerManager.LocalPlayerScript.PlayerFaith.JoinReligion(FaithName));
			}

			if (gameObject == PlayerManager.LocalPlayerObject && FaithName is not "None")
			{
				result.AddElement("Leave Faith",
					() => PlayerManager.LocalPlayerScript.PlayerFaith.LeaveReligion());
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