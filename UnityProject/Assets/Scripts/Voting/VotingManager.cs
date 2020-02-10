using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Controls everything to do with player voting
/// </summary>
public class VotingManager : NetworkBehaviour
{
	public static VotingManager Instance;

	public enum VoteType { RestartRound }
	public enum VotePolicy { MajorityRules }

	private VoteType voteType;
	private VotePolicy votePolicy;
	private bool voteInProgress;
	private float countTime = 0f;
	private int prevSecond = 0;
	private Dictionary<string,bool> votes = new Dictionary<string, bool>();

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

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundStarted, OnRoundStarted);
		EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnded);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundStarted, OnRoundStarted);
		EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnded);
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
	public void TryInitiateRestartVote(GameObject instigator)
	{
		if (voteInProgress || voteRestartSuccess) return;

		if (isCooldownActive)
		{
			Chat.AddExamineMsgFromServer(instigator, $"Too soon to trigger a restart vote!");
			return;
		}

		votes.Clear();
		countTime = 0f;
		prevSecond = 0;
		voteType = VoteType.RestartRound;
		votePolicy = VotePolicy.MajorityRules;
		voteInProgress = true;
		RpcOpenVoteWindow("Vote restart initiated by", instigator.name, CountAmountString(), (30 - prevSecond).ToString());
		Logger.Log($"Vote restart initiated by {instigator.name}", Category.Admin);
	}

	[Server]
	public void RegisterVote(string userId, bool isFor)
	{
		//User already voted
		if (votes.ContainsKey(userId)) return;
		votes.Add(userId, isFor);
		Logger.Log("A user voted: ", Category.Admin);
	}

	void Update()
	{
		if (voteInProgress)
		{
			countTime += Time.deltaTime;

			if (prevSecond != (int) countTime)
			{
				prevSecond = (int) countTime;
				RpcUpdateVoteStats((30 - prevSecond).ToString(), CountAmountString());
				CheckVoteCriteria(true);
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

	private void CheckVoteCriteria(bool stopVoteIfSuccess = false)
	{
		if (IsSuccess(ForVoteCount(), PlayerList.Instance.AllPlayers.Count))
		{
			switch (voteType)
			{
				case VoteType.RestartRound:
					if (voteRestartSuccess) return;
					voteRestartSuccess = true;
					Logger.Log("Vote to restart server was successful. Restarting now.....", Category.Admin);
					GameManager.Instance.EndRound();
					break;
			}

			if (stopVoteIfSuccess)
			{
				voteInProgress = false;
				FinishVote();
			}
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
		return $"{ForVoteCount()} / {PlayerList.Instance.AllPlayers.Count}";
	}

	/// <summary>
	/// Tallies the number of players who have voted 'yes'.
	/// </summary>
	/// <returns>The number of 'yes' votes.</returns>
	private int ForVoteCount()
	{
		int count = 0;
		foreach (bool vote in votes.Values)
		{
			if (vote) count++;
		}
		return count;
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
	private void RpcOpenVoteWindow(string title, string instigator, string count, string time)
	{
		if (GUI_IngameMenu.Instance == null) return;

		GUI_IngameMenu.Instance.VotePopUp.ShowVotePopUp(title, instigator, count, time);
	}
}
