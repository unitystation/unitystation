using System;
using System.Collections;
using HealthV2;
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Items.Tool
{
	public class TRayScanner : NetworkBehaviour, ICheckedInteractable<HandActivate>, ISuicide, IExaminable,
		IItemInOutMovedPlayerButClientTracked
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

		public Mind CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

		public bool IsValidSetup(Mind player)
		{
			if (player != null && pickupable.ItemSlot?.Player == player.CurrentPlayScript.RegisterPlayer)
			{
				//Only turn on goggle for client if they are on
				return currentMode != Mode.Off;
			}

			return false;
		}


		void IItemInOutMovedPlayer.ChangingPlayer(Mind HideForPlayer, Mind ShowForPlayer)
		{
			if (HideForPlayer != null)
			{
				DoRpc(HideForPlayer.CurrentPlayScript.RegisterPlayer, false);
			}

			if (ShowForPlayer != null)
			{
				DoRpc(HideForPlayer.CurrentPlayScript.RegisterPlayer, true);
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
			   pickupable.ItemSlot.Player != PlayerManager.LocalPlayerScript.RegisterPlayer) return;

			DoState(currentMode);
		}

		private void DoState(Mode newMode)
		{
			var matrixInfos = MatrixManager.Instance.ActiveMatricesList;

			foreach (var matrixInfo in matrixInfos)
			{
				var electricalRenderer = matrixInfo.Matrix.ElectricalLayer.GetComponent<TilemapRenderer>();
				var pipeRenderer = matrixInfo.Matrix.PipeLayer.GetComponent<TilemapRenderer>();
				var disposalsRenderer = matrixInfo.Matrix.DisposalsLayer.GetComponent<TilemapRenderer>();

				//Turn them all off
				ChangeState(electricalRenderer, false, 2);
				ChangeState(pipeRenderer, false, 1);
				ChangeState(disposalsRenderer, false);

				switch (newMode)
				{
					case Mode.Off:
						continue;
					case Mode.Wires:
						ChangeState(electricalRenderer, true);
						continue;
					case Mode.Pipes:
						ChangeState(pipeRenderer, true);
						continue;
					case Mode.Disposals:
						ChangeState(disposalsRenderer, true);
						continue;
					default:
						Logger.LogError($"Found no case for {newMode}");
						continue;
				}
			}
		}

		private void ChangeState(TilemapRenderer tileRenderer, bool state, int oldLayerOrder = 0)
		{
			tileRenderer.sortingLayerName = state ? "Walls" : "UnderFloor";
			tileRenderer.sortingOrder = state ? 100 : oldLayerOrder;
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