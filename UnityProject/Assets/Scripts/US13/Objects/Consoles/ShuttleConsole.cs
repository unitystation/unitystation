using System;
using Logs;
using Mirror;
using SecureStuff;
using UnityEngine;
using UnityEngine.Events;
using US13.Core.Addressables.Types;
using US13.Core.Admin.Logs;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Transform;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Messages.Server;
using US13.Objects.Directionals;
using US13.Player;
using US13.Shuttles;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Objects.Shuttles;
using Util;

namespace US13.Objects.Consoles
{
	/// <summary>
	/// Main component for shuttle console
	/// </summary>
	public class ShuttleConsole : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
	{
		//TODO Swapping matrix

		[PlayModeOnly] public MatrixMove ShuttleMatrixMove;


		[NonSerialized] public RegisterTile registerTile;
		private HasNetworkTab hasNetworkTab;

		[SerializeField] private AddressableAudioSource radarDetectionSound;
		public GUI_ShuttleControl GUItab;

		public ShuttleConsoleState shuttleConsoleState;

		public Rotatable Rotatable;

		public bool EngineOn;
		public bool EngineSupport;

		private void Awake()
		{
			hasNetworkTab = GetComponent<HasNetworkTab>();
		}

		private void Start()
		{
			registerTile = GetComponent<RegisterTile>();
			Rotatable = this.GetCachedComponent<Rotatable>();
			ShuttleMatrixMove = GetComponentInParent<MatrixMove>();

			if (ShuttleMatrixMove.NetworkedMatrixMove.ShuttleConsuls.Contains(this) == false)
			{
				ShuttleMatrixMove.NetworkedMatrixMove.ShuttleConsuls.Add(this);
			}
		}

		public void OnDisable()
		{
			ShuttleMatrixMove?.NetworkedMatrixMove?.ShuttleConsuls?.Remove(this);
		}

		public void OnEnable()
		{
			ShuttleMatrixMove = GetComponentInParent<MatrixMove>();
			if (ShuttleMatrixMove  == null) return;
			ShuttleMatrixMove.NetworkedMatrixMove.ShuttleConsuls.Add(this);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			ShuttleMatrixMove = GetComponentInParent<MatrixMove>();

			if (ShuttleMatrixMove == null)
			{
				ShuttleMatrixMove = MatrixManager.Get(registerTile.Matrix).MatrixMove;
				if (ShuttleMatrixMove == null)
				{
					Loggy.Info($"{this} is not on a movable matrix, so won't function.", Category.Shuttles);
					hasNetworkTab.enabled = false;
					return;
				}
				else
				{
					Loggy.Info($"No MatrixMove reference set to {this}, found {ShuttleMatrixMove} automatically",
						Category.Shuttles);
				}
			}

			if (ShuttleMatrixMove.NetworkedMatrixMove.IsNotPilotable)
			{
				hasNetworkTab.enabled = false;
			}
			else
			{
				hasNetworkTab.enabled = true;
			}
		}

		public void PlayRadarDetectionSound()
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(radarDetectionSound, gameObject.AssumedWorldPosServer(),
				default, default, default, default, gameObject);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			//can only be interacted with an emag (normal click behavior is in HasNetTab)
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Emag) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//apply emag
			if (shuttleConsoleState == ShuttleConsoleState.Normal)
			{
				shuttleConsoleState = ShuttleConsoleState.Emagged;
				ServerLogEmagEvent(interaction);
			}
			else if (shuttleConsoleState == ShuttleConsoleState.Emagged)
			{
				shuttleConsoleState = ShuttleConsoleState.Off;
			}
			else if (shuttleConsoleState == ShuttleConsoleState.Off)
			{
				shuttleConsoleState = ShuttleConsoleState.Normal;
			}

			if (GUItab)
			{
				GUItab.OnStateChange(shuttleConsoleState);
			}
		}

		private void ServerLogEmagEvent(HandApply prep)
		{
			AdminLogsManager.AddNewLog(prep.Performer,
				$"{prep.PerformerPlayerScript.playerName} emmaged {gameObject}.", LogCategory.Interaction, Severity.SUSPICOUS);
		}



		[Command(requiresAuthority = false)]
		public void CmdMove(Orientation GlobalMoveDirection, NetworkConnectionToClient sender = null)
		{
			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;
			if (EngineOn == false) return;
			registerTile.Matrix.MatrixMove.NetworkedMatrixMove.RcsMove(GlobalMoveDirection);
		}


		/// <summary>
		/// Connects or disconnects a player from a shuttle rcs
		/// </summary>
		public void ChangeRcsPlayer(bool newState, PlayerScript playerScript)
		{
			var matrixMove = registerTile.Matrix.MatrixMove;

			if (newState)
			{
				PlayerManager.ShuttleConsole = this;
				matrixMove.NetworkedMatrixMove.playerControllingRcs = playerScript;
				matrixMove.NetworkedMatrixMove.RCSModeActive = true;
			}
			else
			{
				PlayerManager.ShuttleConsole = null;
				matrixMove.NetworkedMatrixMove.playerControllingRcs = null;
				matrixMove.NetworkedMatrixMove.RCSModeActive = false;
			}

			//matrixMove.CacheRcs();

			if (isServer)
			{
				if (GUItab)
				{
					GUItab.SetRcsLight(newState);
				}

				if (playerScript && playerScript != PlayerManager.LocalPlayerScript)
				{
					ShuttleRcsMessage.SendTo(this, newState, playerScript.PlayerInfo);
					playerScript.PlayerSync.ResetLocationOnClients();
				}
			}
		}
	}

	public enum ShuttleConsoleState
	{
		Normal,
		Emagged,
		Off
	}

	/// <inheritdoc />
	/// "If you wish to use a generic UnityEvent type you must override the class type."
	[Serializable]
	public class TabStateEvent : UnityEvent<ShuttleConsoleState>
	{
	}
}