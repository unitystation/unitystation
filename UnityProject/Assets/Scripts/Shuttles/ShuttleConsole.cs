using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Main component for shuttle console
/// </summary>
public class ShuttleConsole : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public MatrixMove ShuttleMatrixMove;
	private RegisterTile registerTile;
	private HasNetworkTab hasNetworkTab;

	public TabStateEvent OnStateChange;
	private TabState state = TabState.Normal;

	private List<RcsThruster> bowRcsThrusters = new List<RcsThruster>(); //front
	private List<RcsThruster> sternRcsThrusters = new List<RcsThruster>(); //back
	private List<RcsThruster> portRcsThrusters = new List<RcsThruster>(); //left
	private List<RcsThruster> starBoardRcsThrusters = new List<RcsThruster>(); //right

	public TabState State
	{
		get { return state; }
		set
		{
			if (state != value)
			{
				state = value;
				OnStateChange.Invoke(value);
			}
		}
	}

	private void Awake()
	{
		if (!registerTile)
		{
			registerTile = GetComponent<RegisterTile>();
		}

		hasNetworkTab = GetComponent<HasNetworkTab>();
	}

	private void OnEnable()
	{
		if (ShuttleMatrixMove == null)
		{
			StartCoroutine(InitMatrixMove());
		}
	}

	private IEnumerator InitMatrixMove()
	{
		ShuttleMatrixMove = GetComponentInParent<MatrixMove>();

		if (ShuttleMatrixMove == null)
		{
			while (!registerTile.Matrix)
			{
				yield return WaitFor.EndOfFrame;
			}

			ShuttleMatrixMove = MatrixManager.Get(registerTile.Matrix).MatrixMove;
		}

		if (ShuttleMatrixMove == null)
		{
			Logger.Log($"{this} is not on a movable matrix, so won't function.", Category.Matrix);
			hasNetworkTab.enabled = false;
		}
		else
		{
			Logger.Log($"No MatrixMove reference set to {this}, found {ShuttleMatrixMove} automatically",
				Category.Matrix);
		}

		if (ShuttleMatrixMove != null)
		{
			hasNetworkTab.enabled = true;
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only be interacted with an emag (normal click behavior is in HasNetTab)
		if (!Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Emag)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//apply emag
		switch (State)
		{
			case TabState.Normal:
				State = TabState.Emagged;
				break;
			case TabState.Emagged:
				State = TabState.Off;
				break;
			case TabState.Off:
				State = TabState.Normal;
				break;
		}
	}

	//Searches the matrix for RcsThrusters
	public void CacheRcs()
	{
		ClearRcsCache();
		foreach(Transform t in transform.parent)
		{
			if (t.tag.Equals("Rcs"))
			{
				CacheRcs(t.GetComponent<DirectionalRotatesParent>().MappedOrientation,
					t.GetComponent<RcsThruster>());
			}
		}
	}

	void CacheRcs(OrientationEnum mappedOrientation, RcsThruster thruster)
	{
		var shuttleFacing = ShuttleMatrixMove.InitialFacing;
		if (shuttleFacing == Orientation.Up)
		{
			if(mappedOrientation == OrientationEnum.Up) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) starBoardRcsThrusters.Add(thruster);
		}

		if (shuttleFacing == Orientation.Right)
		{
			if(mappedOrientation == OrientationEnum.Up) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) bowRcsThrusters.Add(thruster);
		}

		if (shuttleFacing == Orientation.Down)
		{
			if(mappedOrientation == OrientationEnum.Up) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) portRcsThrusters.Add(thruster);
		}

		if (shuttleFacing == Orientation.Left)
		{
			if(mappedOrientation == OrientationEnum.Up) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) sternRcsThrusters.Add(thruster);
		}
	}

	void ClearRcsCache()
	{
		bowRcsThrusters.Clear();
		sternRcsThrusters.Clear();
		portRcsThrusters.Clear();
		starBoardRcsThrusters.Clear();
	}
}

public enum TabState
{
	Normal,
	Emagged,
	Off
}

/// <inheritdoc />
/// "If you wish to use a generic UnityEvent type you must override the class type."
[Serializable]
public class TabStateEvent : UnityEvent<TabState>
{
}