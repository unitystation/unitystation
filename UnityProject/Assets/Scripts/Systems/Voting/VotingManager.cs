using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logs;
using Messages.Client.Admin;
using Messages.Server;
using Mirror;
using UnityEngine;
using UI;
using Strings;

/// <summary>
/// Controls everything to do with player voting
/// </summary>
public class VotingManager : NetworkBehaviour
{
	public static VotingManager Instance;

	public enum VotePolicy { MajorityRules }

	private VoteType voteType;
	private VotePolicy votePolicy;
	private bool voteInProgress;
	private float countTime = 0f;
	private int prevSecond = 0;
	private Dictionary<string,string> votes = new Dictionary<string, string>();

	private bool voteRestartSuccess = false;

	/// <summary>
	/// The initial time during a round before a vote can be started
	/// </summary>
	[SerializeField]
	private float RoundStartCooldownTime = 60f;

	/// <summary>
	/// The cooldown time between votes
	/// </summary>
	[SerializeField]
	private float CooldownTime = 60f;

	/// <summary>
	/// Is voting disabled due to the cooldown being active?
	/// </summary>
	private bool isCooldownActive;

	/// <summary>
	/// The active cooldown Coroutine
	/// </summary>
	private Coroutine cooldown;

	private List<string> MapList = new List<string>();
	private List<string> GameModeList = new List<string>();
	private List<string> yesNoList = new List<string>();

	private List<string> possibleVotes = new List<string>();

	public enum VoteType
	{
		RestartRound,
		NextGameMode,
		NextMap
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void Start()
	{
		MapList = SubSceneManager.Instance.MainStationList.GetMaps();
		GameModeList = GameManager.Instance.GetAvailableGameModeNames();
		yesNoList.Add("Yes");
		yesNoList.Add("No");
		if (Application.isEditor) RoundStartCooldownTime = 5f;
	}

	void OnEnable()
	{
		EventManager.AddHandler(Event.RoundStarted, OnRoundStarted);
		EventManager.AddHandler(Event.RoundEnded, OnRoundEnded);
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(Event.RoundStarted, OnRoundStarted);
		EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnded);
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	void OnRoundStarted()
	{
		cooldown = StartCoroutine(StartVoteCooldown(RoundStartCooldownTime));
	}

	void OnRoundEnded()
	{
		if (cooldown != null)
		{
			StopCoroutine(cooldown);
		}
	}

	[Server]
	public void TryInitiateRestartVote(GameObject instigator, NetworkConnection sender = null)
	{
		SetupVote(VoteType.RestartRound, VotePolicy.MajorityRules, 30, instigator, sender);
	}

	[Server]
	public void TryInitiateNextGameModeVote(GameObject instigator, NetworkConnection sender = null)
	{
		SetupVote(VoteType.NextGameMode, VotePolicy.MajorityRules, 30, instigator, sender);
	}

	[Server]
	public void TryInitiateNextMapVote(GameObject instigator, NetworkConnection sender = null)
	{
		SetupVote(VoteType.NextMap, VotePolicy.MajorityRules, 30, instigator, sender);
	}

	public void SetupVote(VoteType type, VotePolicy policy, int time, GameObject instigator, NetworkConnection sender)
	{
		if (voteInProgress || voteRestartSuccess) return;

		if (isCooldownActive)
		{
			Chat.AddExamineMsgFromServer(instigator, $"Too soon to trigger a vote!");
			return;
		}

		votes.Clear();
		possibleVotes.Clear();
		countTime = 0f;
		prevSecond = 0;
		voteType = type;
		votePolicy = policy;
		voteInProgress = true;
		switch (type)
		{
			case VoteType.RestartRound:
				possibleVotes.AddRange(yesNoList);
				RpcOpenVoteWindow("Voting for round restart initiated by", instigator.name, CountAmountString(), (time - prevSecond).ToString(), yesNoList);
				break;
			case VoteType.NextGameMode:
				possibleVotes.AddRange(GameModeList);
				RpcOpenVoteWindow("Voting for next Game Mode initiated by", instigator.name, CountAmountString(), (time - prevSecond).ToString(), GameModeList);
				break;
			case VoteType.NextMap:
				possibleVotes.AddRange(MapList);
				RpcOpenVoteWindow("Voting for next map initiated by", instigator.name, CountAmountString(), (time - prevSecond).ToString(), MapList);
				break;
		}

		if (sender != null)
		{
			RpcVoteCallerDefault(sender);
		}

		Loggy.Log($"Vote initiated by {instigator.name}", Category.Admin);
	}

	/// <summary>
	/// I only made this a function for the reference
	/// </summary>
	private bool CheckForSussyVote(string isFor)
	{
		return possibleVotes.Contains(isFor);
	}

	[Server]
	public void RegisterVote(string userId, string isFor)
	{
		if(CheckForSussyVote(isFor) == false) return; //User is cheating.

		//If user has vote change vote if different, else add to vote list
		if (votes.ContainsKey(userId))
		{
			votes.TryGetValue(userId, out string value);
			if (value == isFor) return;

			votes[userId] = isFor;
		}
		else
		{
			votes.Add(userId, isFor);
		}
		Loggy.Log($"A user: {userId} voted: {isFor}", Category.Admin);
	}

	[Server]
	public void VetoVote(PlayerInfo admin)
	{
		voteInProgress = false;
		FinishVote();
		votes.Clear();

		Chat.AddGameWideSystemMsgToChat($"<color={ChatTemplates.Blue}>Vote was vetoed by an admin.</color>");

		var msg = $"Vote was vetoed by {admin.Username}.";

		UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(msg, admin.UserId);
		Loggy.Log(msg, Category.Admin);
	}

	void UpdateMe()
	{
		if (voteInProgress)
		{
			countTime += Time.deltaTime;

			if (prevSecond != (int) countTime)
			{
				prevSecond = (int) countTime;
				RpcUpdateVoteStats((30 - prevSecond).ToString(), CountAmountString());

				//If there are admins online, dont complete vote until after 15 seconds even if it will pass to allow for veto
				if (PlayerList.Instance.GetAllAdmins().Count > 0 && (30 - prevSecond) > 15) return;

				CheckVoteCriteria();
			}

			if (countTime > 30f)
			{
				voteInProgress = false;
				CheckVoteCriteria();
				FinishVote();
			}
		}
	}

	/// <summary>
	/// Coroutine to time the cooldown period
	/// </summary>
	private IEnumerator StartVoteCooldown(float time)
	{
		isCooldownActive = true;
		yield return WaitFor.Seconds(time);
		isCooldownActive = false;
	}

	private void CheckVoteCriteria()
	{
		if (IsSuccess(votes.Count, PlayerList.Instance.AllPlayers.Count))
		{
			var winner = GetHighestVote();
			if (winner == "")
			{
				Chat.AddGameWideSystemMsgToChat($"<color=blue>Voting failed! vote has somehow passed but no winner was written!</color>");
				return;
			}
			switch (voteType)
			{
				case VoteType.RestartRound:
					if (winner == "No")
					{
						Chat.AddGameWideSystemMsgToChat($"<color=blue>Voting failed! Not enough people voted to restart");
						return;
					}
					if (GameManager.Instance.CurrentRoundState != RoundState.Started) return;
					Loggy.Log("Vote to restart server was successful. Restarting now.....", Category.Admin);
					VideoPlayerMessage.Send(VideoType.RestartRound);
					GameManager.Instance.RoundEndTime = 5; // Quick round end when triggered by Players.

					GameManager.Instance.EndRound();
					break;
				case VoteType.NextGameMode:
					Chat.AddGameWideSystemMsgToChat($"<color=blue>Vote passed! Next GameMode will be {winner}</color>");
					GameManager.Instance.NextGameMode = winner;
					GameManager.Instance.SecretGameMode = false;
					break;
				case VoteType.NextMap:
					Chat.AddGameWideSystemMsgToChat($"<color=blue>Vote passed! Next map will be {winner}</color>");
					SubSceneManager.AdminForcedMainStation = winner;
					break;
			}

			voteInProgress = false;
			FinishVote();
		}
	}

	private bool IsSuccess(int forVotes, int maxVoters)
	{
		switch (votePolicy)
		{
			case VotePolicy.MajorityRules:
				if (forVotes > (maxVoters / 2))
				{
					return true;
				}
				break;
		}

		return false;
	}

	private void FinishVote()
	{
		StartCoroutine(StartVoteCooldown(CooldownTime));
		RpcFinishVote();
	}

	private string CountAmountString()
	{
		return $"{votes.Count} / {PlayerList.Instance.AllPlayers.Count}";
	}

	/// <summary>
	/// Gets the highest vote count on the list
	/// </summary>
	/// <returns></returns>
	private string GetHighestVote()
	{
		Dictionary<string, int> count = new Dictionary<string, int>();
		var highestVote = 0;
		var winner = "";
		foreach (var vote in votes)
		{
			if (count.ContainsKey(vote.Value) == false)
			{
				count.Add(vote.Value, 0);
			}
			count[vote.Value] += 1;
			if (count[vote.Value] < highestVote) continue;
			highestVote = count[vote.Value];
			winner = vote.Value;
		}

		return winner;
	}

	[ClientRpc]
	private void RpcUpdateVoteStats(string countDown, string voteCount)
	{
		//Update UI
		if (GUI_IngameMenu.Instance == null) return;

		GUI_IngameMenu.Instance.VotePopUp.UpdateVoteWindow(voteCount, countDown);
	}

	[ClientRpc]
	private void RpcFinishVote()
	{
		//Close vote window if open
		if (GUI_IngameMenu.Instance == null) return;

		GUI_IngameMenu.Instance.VotePopUp.CloseVoteWindow();
	}

	[ClientRpc]
	private void RpcOpenVoteWindow(string title, string instigator, string count, string time, List<string> options)
	{
		if (GUI_IngameMenu.Instance == null) return;

		GUI_IngameMenu.Instance.VotePopUp.ShowVotePopUp(title, instigator, count, time, options);
	}

	[TargetRpc]
	private async void RpcVoteCallerDefault(NetworkConnection target)
	{
		if (GUI_IngameMenu.Instance == null || PlayerList.Instance.AllPlayers.Count == 1) return;

		await Task.Delay(500); //Away for the list to be generated before automatically voting
		GUI_IngameMenu.Instance.VotePopUp.VoteYes();
	}
}
