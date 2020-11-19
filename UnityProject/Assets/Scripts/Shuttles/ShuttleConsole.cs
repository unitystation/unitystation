using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Objects.Shuttles
{
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

		public TabState State {
			get { return state; }
			set {
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

			if (ShuttleMatrixMove.IsNotPilotable)
			{
				hasNetworkTab.enabled = false;
			}
			else if (ShuttleMatrixMove != null)
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
}
