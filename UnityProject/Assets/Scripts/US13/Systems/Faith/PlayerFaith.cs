using System;
using Logs;
using Mirror;
using UnityEngine;
using US13.Actions;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.Items.Others;
using US13.Items.Traits;
using US13.Player;
using US13.Systems.Communications;
using US13.Systems.Inventory;
using US13.UI.Systems;
using Util;
using Util.Independent.FluentRichText;

namespace US13.Systems.Faith
{
	public class PlayerFaith : NetworkBehaviour, IChatInfluencer
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

		public GameObject Paper;
		public ItemTrait BibleTrait;

		[Server]
		public void JoinReligion(Faith newFaith)
		{
			if (newFaith == null)
			{
				Loggy.Error("[PlayerFaith] - Cannot join a null faith.");
				return;
			}
			currentFaith = newFaith;
			FaithName = currentFaith.FaithName;
			FaithManager.JoinFaith(newFaith, player);
			Chat.AddExamineMsg(gameObject, $"You've put your faith in <i>{FaithName}</i>.");
			try
			{
				if (player.Mind?.occupation != null && player.Mind.occupation.DisplayName == "Chaplain")
				{
					ShovePaperInsideBible(player);
				}
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}
		}

		private void ShovePaperInsideBible(PlayerScript target)
		{
			var spawnedPaper = Spawn.ServerPrefab(Paper).GameObject;
			var paperText = spawnedPaper.GetComponent<Paper>();
			paperText.SetServerString($"Faith: {currentFaith.FaithName}\n\nProclamation: {currentFaith.ProclamationText}\n\nRejection: {currentFaith.RejectionText}");
			var handslots = target.DynamicItemStorage.GetHandSlots();
			foreach (var slot in handslots)
			{
				if (slot.IsEmpty) continue;
				if (slot.ItemAttributes.HasTrait(BibleTrait) == false) continue;
				var storage = slot.ItemObject.GetComponent<ItemStorage>();
				if (storage.ServerTryAdd(spawnedPaper)) return;
			}
			spawnedPaper.GetUniversalObjectPhysics().AppearAtWorldPositionServer(target.WorldPos);
		}

		[Command]
		public void JoinReligion(string newFaith)
		{
			JoinReligion(FaithManager.Instance.AllFaiths.Find(x => x.Faith.FaithName == newFaith).Faith);
		}

		[Command]
		public void LeaveReligion()
		{
			FaithManager.LeaveFaith(player);
			currentFaith = null;
			FaithName = "None";
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

		public string ToleranceCheckForReligion()
		{
			//This is client trickery, anything we want to check on the client itself is from PlayerManager
			//while things on the other player is done directly from within this class
			if (PlayerManager.LocalPlayerScript?.PlayerFaith?.currentFaith == null) return "";
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

		private void CheckForProclamationOfFaith(ref ChatEvent proclaimerEvent)
		{
			if ( CurrentFaith.ProclamationText.ToLower().TrimEnd() != proclaimerEvent.message.ToLower().TrimEnd() ) return;
			if (proclaimerEvent.originator.TryGetComponent<PlayerFaith>(out var playerFaith) == false)
			{
				Loggy.Error($"Could not find PlayerFaith component on {proclaimerEvent.originator.name} when checking for joining religion.");
				return;
			}
			playerFaith.JoinReligion(CurrentFaith);
			proclaimerEvent.message.Color(Color.green);
		}

		private void CheckForRemovalOfFaith(ref ChatEvent removal)
		{
			if ( CurrentFaith.RejectionText.ToLower().TrimEnd() != removal.message.ToLower().TrimEnd() ) return;
			if (removal.originator.TryGetComponent<PlayerFaith>(out var playerFaith) == false)
			{
				Loggy.Error($"Could not find PlayerFaith component on {removal.originator.name} when checking for removing religion.");
				return;
			}
			if (playerFaith.FaithName == "None") return;
			playerFaith.LeaveReligion();
			Chat.AddExamineMsg(playerFaith.gameObject, "You have left your faith.");
			removal.message.Color(Color.red);
		}

		public bool WillInfluenceChat()
		{
			return CurrentFaith != null;
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			CheckForRemovalOfFaith(ref chatToManipulate);
			if (CurrentFaith != null) CheckForProclamationOfFaith(ref chatToManipulate);
			return chatToManipulate;
		}
	}
}