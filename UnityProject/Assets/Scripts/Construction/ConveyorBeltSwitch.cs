using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using UnityEngine;

public class ConveyorBeltSwitch : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public SpriteRenderer spriteRenderer;

	public ConveyorBelt[] conveyorBelts;

	public Sprite spriteForward;
	public Sprite spriteBackward;
	public Sprite spriteOff;

	private Sprite LastSprite;

	[SyncVar(hook = nameof(SyncSwitchState))]
	public State currentState = State.Off;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side)
		       && Validations.IsTarget(gameObject, interaction);
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
			conveyor.UpdateStatus(currentState);
		}
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		switch (currentState)
		{
			case State.Off:
				currentState = State.Forward;
				break;
			case State.Forward:
				currentState = State.Backward;
				break;
			case State.Backward:
				currentState = State.Off;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public enum State
	{
		Off = 0,
		Forward = 1,
		Backward = 2
	}
}
