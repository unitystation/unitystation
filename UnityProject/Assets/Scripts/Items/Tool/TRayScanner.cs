using System;
using System.Collections;
using HealthV2;
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Items.Tool
{
	public class TRayScanner : NetworkBehaviour, ICheckedInteractable<HandActivate>, ISuicide, IExaminable,
		IServerInventoryMove, IOnPlayerRejoin, IOnPlayerTransfer, IOnPlayerLeaveBody
	{
		[SyncVar(hook = nameof(SyncMode))] private Mode currentMode = Mode.Off;

		private SpriteHandler spriteHandler;

		private Pickupable pickupable;

		private Array types;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			types = Enum.GetValues(typeof(Mode));
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			IncrementMode();

			if (currentMode == Mode.Off)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You switch the {gameObject.ExpensiveName()} off");
				spriteHandler.ChangeSprite(0);
				return;
			}

			Chat.AddExamineMsgFromServer(interaction.Performer,
				$"You switch the {gameObject.ExpensiveName()} to detect {currentMode.ToString()}");
			spriteHandler.ChangeSprite(1);
		}

		private void IncrementMode()
		{
			var currentModeInt = (int)currentMode;
			currentModeInt++;

			if (currentModeInt > (types.Length - 1))
			{
				currentModeInt = 0;
			}

			currentMode = (Mode)currentModeInt;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			//From a player
			if (info.FromPlayer != null)
			{
				//Removed or put in backpack
				if (info.ToSlot == null || info.ToSlot.Player == null)
				{
					//Turn off
					DoRpc(info.FromPlayer, false);
				}
			}

			if(info.FromPlayer == info.ToPlayer) return;

			//Put on ground dont need to go further
			if(info.ToSlot == null) return;

			//To new player
			if (info.ToPlayer != null)
			{
				//Put in backpack
				if (info.ToSlot.Player == null)
				{
					//Should already be off so dont need to do anything
					return;
				}

				//Ping to turn on if needed
				DoRpc(info.ToPlayer, currentMode != Mode.Off);
			}
		}

		private void DoRpc(RegisterPlayer player, bool newState)
		{
			if(player.connectionToClient == null) return;

			if(CustomNetworkManager.IsServer && CustomNetworkManager.IsHeadless == false)
			{
				//Target RPC not working on local host?
				if (newState)
				{
					DoState(currentMode);
					return;
				}

				DoState(Mode.Off);
				return;
			}

			RpcChangeState(player.connectionToClient, newState);
		}

		[TargetRpc]
		private void RpcChangeState(NetworkConnection conn, bool newState)
		{
			if (newState)
			{
				DoState(currentMode);
				return;
			}

			DoState(Mode.Off);
		}

		private void SyncMode(Mode oldMode, Mode newMode)
		{
			currentMode = newMode;
			if(PlayerManager.LocalPlayerScript == null) return;

			if(pickupable.ItemSlot == null || pickupable.ItemSlot.Player == null ||
			   pickupable.ItemSlot.Player != PlayerManager.LocalPlayerScript.registerTile) return;

			DoState(currentMode);
		}

		private void DoState(Mode newMode)
		{
			var matrixInfos = MatrixManager.Instance.ActiveMatricesList;

			//TODO update for layer PR

			foreach (var matrixInfo in matrixInfos)
			{
				var tilemapRenderer = matrixInfo.Matrix.UnderFloorLayer.GetComponent<TilemapRenderer>();
				tilemapRenderer.sortingLayerName = newMode == Mode.Off ? "UnderFloor" : "Walls";
				tilemapRenderer.sortingOrder = newMode == Mode.Off ? 0 : 1;
			}
		}

		public void OnPlayerRejoin()
		{
			if(pickupable.ItemSlot?.Player == null) return;

			DoRpc(pickupable.ItemSlot.Player, currentMode != Mode.Off);
		}

		public void OnPlayerTransfer()
		{
			if(pickupable.ItemSlot?.Player == null) return;

			DoRpc(pickupable.ItemSlot.Player, currentMode != Mode.Off);
		}

		public void OnPlayerLeaveBody()
		{
			if(pickupable.ItemSlot?.Player == null) return;

			DoRpc(pickupable.ItemSlot.Player, false);
		}

		private enum Mode
		{
			Off,
			Wires,
			Pipes,
			Disposals
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (currentMode == Mode.Off)
			{
				return $"The {gameObject.ExpensiveName()} is off!\n A label reads, \"do not point at living tissue\"";
			}

			return $"The {gameObject.ExpensiveName()} is currently detecting {currentMode.ToString()}.\n A label reads, \"do not point at living tissue\"";
		}

		#region Suicide

		public bool CanSuicide(GameObject performer)
		{
			return true;
		}

		public IEnumerator OnSuicide(GameObject performer)
		{
			if (performer.TryGetComponent<LivingHealthMasterBase>(out var player) == false) yield break;

			Chat.AddActionMsgToChat(performer, $"You begin to emit terahertz-rays into your brain with the {gameObject.ExpensiveName()}!",
				$"{performer.ExpensiveName()} begins to emit terahertz-rays into {performer.GetTheirPronoun()} brain with the {gameObject.ExpensiveName()}! It looks like {performer.GetTheyrePronoun()} trying to commit suicide!");

			yield return WaitFor.Seconds(2);
			if(player == null) yield break;

			player.ApplyDamageToBodyPart(performer, 500f, AttackType.Bio, DamageType.Radiation, BodyPartType.Head);
			player.Death();
		}

		#endregion
	}
}