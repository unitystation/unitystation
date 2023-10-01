
using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;

/// <summary>
/// Component which can take on an arbitrary state from a list of possible states, and the state is synced
/// over the network.
/// </summary>
public class Stateful : NetworkBehaviour, IServerSpawn
{

	[Tooltip("Required. Initial state when this object spawns. Must reference a state which exists in States.")]
	[SerializeField]
	private StatefulState initialState = null;

	[Tooltip("Possible states this object can be in.")]
	[SerializeField]
	private List<StatefulState> states = null;

	//tracks the index of the state we are currently in.
	[SyncVar(hook = nameof(SyncCurrentStateIndex))]
	private int currentStateIndex = -1;

	private StatefulState currentState;

	/// <summary>
	/// Current state.
	/// </summary>
	public StatefulState CurrentState => currentState;


	public override void OnStartClient()
	{
		SyncCurrentStateIndex(currentStateIndex, currentStateIndex);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		//start in initial state
		if (initialState == null)
		{
			Loggy.LogErrorFormat("Initial State not defined for {0}. Please fix this component.", Category.ItemSpawn,
				this);
			return;
		}
		var initialStateIndex = states.FindIndex(se => se == initialState);
		if (initialStateIndex == -1)
		{
			Loggy.LogErrorFormat("Initial State doesn't exist in States defined for {0}. Please fix this component.", Category.ItemSpawn,
				this);
			return;
		}

		SyncCurrentStateIndex(currentStateIndex, initialStateIndex);
	}


	private void SyncCurrentStateIndex(int oldStateIndex, int newStateIndex)
	{
		currentStateIndex = newStateIndex;
		if (currentStateIndex >= 0)
		{
			currentState = states[currentStateIndex];
		}
	}

	/// <summary>
	/// Change the current state to the indicated one. State must have an entry in
	/// States.
	/// </summary>
	/// <param name="newState"></param>
	[Server]
	public void ServerChangeState(StatefulState newState)
	{
		var newStateIndex = states.FindIndex(se => se == newState);
		if (newStateIndex == -1)
		{
			Loggy.LogErrorFormat("New state doesn't exist in States defined for {0}. State will not be changed.", Category.Objects,
				this);
			return;
		}
		SyncCurrentStateIndex(currentStateIndex, newStateIndex);
	}
}
