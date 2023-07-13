using System;
using ScriptableObjects;
using UnityEngine;

namespace Objects.Wallmounts
{
	public class CameraAssembly : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private GameObject securityCameraPrefab = null;

		[SerializeField]
		private GameObject securityCameraItemPrefab = null;

		private RegisterTile registerTile;
		private Rotatable rotatable;

		private CameraAssemblyState state = CameraAssemblyState.Unwelded;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			rotatable = GetComponent<Rotatable>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//Weld, or wrench to remove from wall
			if (Validations.HasUsedActiveWelder(interaction) ||
			    Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench)) return true;

			//Wire or unweld from wall
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) ||
			    Validations.HasUsedActiveWelder(interaction)) return true;

			//TODO upgrades

			//Screwdrive
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
			    Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (state == CameraAssemblyState.Unwelded)
			{
				if (Validations.HasUsedActiveWelder(interaction))
				{
					//Weld onto wall
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start welding the camera assembly onto the wall...",
						$"{interaction.Performer.ExpensiveName()} starts welding the camera assembly onto the wall...",
						"You weld the camera assembly onto the wall.",
						$"{interaction.Performer.ExpensiveName()} welds the camera assembly onto the wall.",
						() =>
						{
							SetState(CameraAssemblyState.Welded);
						});

					return;
				}

				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					//Wrench from wall
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start wrenching the camera assembly from the wall...",
						$"{interaction.Performer.ExpensiveName()} starts wrenching the camera assembly from the wall...",
						"You wrench the camera assembly from the wall.",
						$"{interaction.Performer.ExpensiveName()} wrenchs the camera assembly from the wall.",
						() =>
						{
							Spawn.ServerPrefab(securityCameraItemPrefab, registerTile.WorldPositionServer,
								transform.parent);

							_ = Despawn.ServerSingle(gameObject);
						});

					return;
				}

				return;
			}

			if (state == CameraAssemblyState.Welded)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable))
				{
					//Add cable
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding cable to the camera assembly...",
						$"{interaction.Performer.ExpensiveName()} starts adding cable to the camera assembly...",
						"You add cable to the camera assembly.",
						$"{interaction.Performer.ExpensiveName()} adds cable to the camera assembly.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 1);
							SetState(CameraAssemblyState.Wired);
						});

					return;
				}

				if (Validations.HasUsedActiveWelder(interaction))
				{
					//Unweld from wall
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start unwelding the camera assembly from the wall...",
						$"{interaction.Performer.ExpensiveName()} starts unwelding the camera assembly from the wall...",
						"You unweld the camera assembly onto the wall.",
						$"{interaction.Performer.ExpensiveName()} unwelds the camera assembly from the wall.",
						() =>
						{
							SetState(CameraAssemblyState.Unwelded);
						});

					return;
				}

				return;
			}

			if (state == CameraAssemblyState.Wired)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					//Screwdrive shut
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start closing the panel on the camera assembly...",
						$"{interaction.Performer.ExpensiveName()} starts closing the panel on the camera assembly...",
						"You close the panel on the camera assembly.",
						$"{interaction.Performer.ExpensiveName()} closes the panel on the camera assembly.",
						() =>
						{
							var result = Spawn.ServerPrefab(securityCameraPrefab, registerTile.WorldPositionServer,
								transform.parent);

							if (result.Successful)
							{
								result.GameObject.GetComponent<Rotatable>().FaceDirection(rotatable.CurrentDirection);
								result.GameObject.GetComponent<SecurityCamera>().SetUp(interaction.PerformerPlayerScript);
							}

							_ = Despawn.ServerSingle(gameObject);
						});

					return;
				}

				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter))
				{
					//Cut cable
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start cutting the cable from the camera assembly...",
						$"{interaction.Performer.ExpensiveName()} starts cutting the cable from the camera assembly...",
						"You cut the cable from the camera assembly.",
						$"{interaction.Performer.ExpensiveName()} cuts the cable from the camera assembly.",
						() =>
						{
							Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, registerTile.WorldPositionServer, transform.parent);
							SetState(CameraAssemblyState.Welded);
						});
				}
			}
		}

		public void SetState(CameraAssemblyState newState)
		{
			state = newState;
		}

		public enum CameraAssemblyState
		{
			Unwelded,
			Welded,
			Wired
		}
	}
}
