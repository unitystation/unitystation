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

	[Server]
	public void TryInitiateRestartVote()
	{
		if (voteInProgress || voteRestartSuccess) return;

		votes.Clear();
		countTime = 0f;
		prevSecond = 0;
		voteType = VoteType.RestartRound;
		votePolicy = VotePolicy.MajorityRules;
		voteInProgress = true;
	}

	[Server]
	public void RegisterRestartVote(string userId, bool isFor)
	{
		//User already voted
		if (votes.ContainsKey(userId)) return;
		votes.Add(userId, isFor);
	}

	void Update()
	{
		if (voteInProgress)
		{
			countTime += Time.deltaTime;

			if (prevSecond != (int) countTime)
			{
				RpcUpdateVoteStats(prevSecond, ForVoteCount(), PlayerList.Instance.AllPlayers.Count);
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

	private void CheckVoteCriteria(bool stopVoteIfSuccess = false)
	{
		if (IsSuccess(ForVoteCount(), PlayerList.Instance.AllPlayers.Count))
		{
			switch (voteType)
			{
				case VoteType.RestartRound:
					if (voteRestartSuccess) return;
					voteRestartSuccess = true;
					StartCoroutine(BeginRoundRestart());
					Logger.Log("Vote too restart server was successful. Restarting now.....", Category.Admin);
					break;
			}

			if (stopVoteIfSuccess)
			{
				voteInProgress = false;
				FinishVote();
			}
		}
	}

	IEnumerator BeginRoundRestart()
	{
		//A healthy delay before restarting everything
		yield return WaitFor.Seconds(2f);
		GameManager.Instance.RestartRound();
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
		RpcFinishVote();
	}

	private int ForVoteCount()
	{
		var getAllForVotes = votes.Values.Select(x => true);
		return getAllForVotes.Count();
	}

	[ClientRpc]
	private void RpcUpdateVoteStats(int currentSecond, int votesFor, int maxVoters)
	{
		//Update UI
	}

	[ClientRpc]
	private void RpcFinishVote()
	{
		//Close vote window if open
	}
}
