using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ConveyorBeltSwitch : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public SpriteRenderer spriteRenderer;

	public List<ConveyorBelt> conveyorBelts = new List<ConveyorBelt>();

	public Sprite spriteForward;
	public Sprite spriteBackward;
	public Sprite spriteOff;

	private float timeElapsed = 0;

	[SerializeField] private float ConveyorBeltSpeed = 0.5f;

	[SyncVar(hook = nameof(SyncSwitchState))]
	public State currentState;

	private State prevMoveState;

	public override void OnStartServer()
	{
		currentState = State.Off;
	}

	public override void OnStartClient()
	{
		SyncSwitchState(currentState, currentState);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!Validations.IsTarget(gameObject, interaction)) return false;

		return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
		       interaction.HandObject == null ||
		       Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar);
	}

	private void SyncSwitchState(State oldValue, State newValue)
	{
		currentState = newValue;

		switch (currentState)
		{
			case State.Off:
				spriteRenderer.sprite = spriteOff;
				break;
			case State.Forward:
				spriteRenderer.sprite = spriteForward;
				break;
			case State.Backward:
				spriteRenderer.sprite = spriteBackward;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		foreach (ConveyorBelt conveyor in conveyorBelts)
		{
			if (conveyor != null)
			{
				conveyor.UpdateStatus(currentState); //sync clients
			}
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	protected virtual void UpdateMe()
	{
		if (currentState == State.Off) return;

		timeElapsed += Time.deltaTime;
		if (timeElapsed > ConveyorBeltSpeed)
		{
			MoveConveyorBelt();
			timeElapsed = 0;
		}
	}

	void MoveConveyorBelt()
	{
		for (int i = 0; i < conveyorBelts.Count; i++)
		{
			if (conveyorBelts[i] != null) conveyorBelts[i].MoveBelt();
		}
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver)) //clearing conveyor list
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
				"You start clearing the connected conveyor belts...",
				$"{interaction.Performer.ExpensiveName()} starts clearing the connected conveyor belts...",
				"You clear the connected the conveyor belts.",
				$"{interaction.Performer.ExpensiveName()} clears the connected conveyor belts.",
				() =>
				{
					currentState = State.Off;
					spriteRenderer.sprite = spriteOff;
					conveyorBelts.Clear();
				});
		}
		else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar))
		{
			//deconsruct
			ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
				"You start deconstructing the conveyor belt switch...",
				$"{interaction.Performer.ExpensiveName()} starts deconstructing the conveyor belt switch...",
				"You deconstruct the conveyor belt switch.",
				$"{interaction.Performer.ExpensiveName()} deconstructs the conveyor belt switch.",
				() =>
				{
					currentState = State.Off;
					spriteRenderer.sprite = spriteOff;
					conveyorBelts.Clear();
					Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 5);
					Despawn.ServerSingle(gameObject);
				});
		}
		else
		{
			switch (currentState)
			{
				case State.Off:
					if (prevMoveState == State.Forward)
					{
						currentState = State.Backward;
					}
					else if (prevMoveState == State.Backward)
					{
						currentState = State.Forward;
					}
					else
					{
						currentState = State.Forward;
					}
					prevMoveState = currentState;
					break;
				case State.Forward:
				case State.Backward:
					currentState = State.Off;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	//Multitool buffer adding
	public void AddConveyorBelt(List<ConveyorBelt> newConveyorBelts)
	{
		foreach (var conveyor in newConveyorBelts)
		{
			if (!conveyorBelts.Contains(conveyor))
			{
				conveyorBelts.Add(conveyor);
			}
		}
	}

	public enum State
	{
		Off = 0,
		Forward = 1,
		Backward = 2
	}
}