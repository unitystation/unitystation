using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Objects.Wallmounts;
using Random = UnityEngine.Random;

namespace Objects
{
	[RequireComponent(typeof(GeneralSwitchController))]
	[ExecuteInEditMode]
	public class MassDriver : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		private const float ANIMATION_TIME = 0.5f;

		private RegisterTile registerTile;

		[SerializeField]
		private Rotatable directional = default;

		private Matrix Matrix => registerTile.Matrix;

		private SpriteHandler spriteHandler;

		private GeneralSwitchController switchController;

		bool massDriverOperating = false;

		[SyncVar(hook = nameof(OnSyncOrientation))]
		private OrientationEnum orientation;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			switchController = GetComponent<GeneralSwitchController>();
		}

		private void OnEnable()
		{
			switchController.SwitchPressedDoAction.AddListener(DoAction);

			if (directional != null)
			{
				directional.OnRotationChange.AddListener(OnDirectionChanged);
			}
		}

		private void OnDisable()
		{
			switchController.SwitchPressedDoAction.RemoveListener(DoAction);

			if (directional != null)
			{
				directional.OnRotationChange.RemoveListener(OnDirectionChanged);
			}
		}

		#region Sync

		private void UpdateSpriteOutletState()
		{
			if (massDriverOperating) spriteHandler.ChangeSprite(1);
			else spriteHandler.ChangeSprite(0);
		}

		private void OnDirectionChanged(OrientationEnum newDir)
		{
			orientation = newDir;
		}

		private void OnSyncOrientation(OrientationEnum oldState, OrientationEnum newState)
		{
			orientation = newState;
			UpdateSpriteOrientation();
		}

		public void EditorUpdate()
		{
			orientation = directional.CurrentDirection;
			UpdateSpriteOrientation();
		}

		private void UpdateSpriteOrientation()
		{
			switch (orientation)
			{
				case OrientationEnum.Up_By0:
					spriteHandler.ChangeSpriteVariant(1);
					break;
				case OrientationEnum.Down_By180:
					spriteHandler.ChangeSpriteVariant(0);
					break;
				case OrientationEnum.Left_By90:
					spriteHandler.ChangeSpriteVariant(3);
					break;
				case OrientationEnum.Right_By270:
					spriteHandler.ChangeSpriteVariant(2);
					break;
			}
		}

		#endregion

		public void DoAction()
		{
			if (!CustomNetworkManager.IsServer) return;

			StartCoroutine(DetectObjects());
		}

		private IEnumerator DetectObjects()
		{
			massDriverOperating = true;
			UpdateSpriteOutletState();
			//detect players positioned on the mass driver
			var playersFound = Matrix.Get<UniversalObjectPhysics>(registerTile.LocalPositionServer, ObjectType.Player, true);

			var throwVector = orientation.ToLocalVector3();

			foreach (var player in playersFound)
			{
				// Players cannot currently be thrown, so just push them in the direction for now.
				PushPlayer(player, throwVector);
			}

			foreach (var objects in Matrix.Get<UniversalObjectPhysics>(registerTile.LocalPositionServer, ObjectType.Object, true))
			{
				// Objects cannot currently be thrown, so just push them in the direction for now.
				PushObject(objects, throwVector);
			}

			foreach (var item in Matrix.Get<UniversalObjectPhysics>(registerTile.LocalPositionServer, ObjectType.Item, true))
			{
				ThrowItem(item, throwVector);
			}

			yield return WaitFor.Seconds(ANIMATION_TIME);

			massDriverOperating = false;
			UpdateSpriteOutletState();
		}

		private void ThrowItem(UniversalObjectPhysics item, Vector3 throwVector)
		{
			Vector3 vector = item.transform.rotation * throwVector;
			UniversalObjectPhysics itemTransform = item.GetComponent<UniversalObjectPhysics>();
			if (itemTransform == null) return;
			itemTransform.NewtonianPush(vector, 30, inAim:BodyPartType.Chest ,inThrownBy : gameObject );
		}

		private void PushObject(UniversalObjectPhysics entity, Vector3 pushVector)
		{
			//Push Twice
			entity.NewtonianPush(pushVector.NormalizeTo2Int(), 30);
		}

		private void PushPlayer(UniversalObjectPhysics player, Vector3 pushVector)
		{
			player.GetComponent<RegisterPlayer>()?.ServerStun();

			//Push Twice
			player.NewtonianPush(pushVector.NormalizeTo2Int(), 30);
		}

		#region WrenchChangeDirection

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.IsTarget(gameObject, interaction)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null) return;

			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			{
				switch (orientation)
				{
					case OrientationEnum.Right_By270:
						directional.FaceDirection(OrientationEnum.Up_By0);
						break;
					case OrientationEnum.Up_By0:
						directional.FaceDirection(OrientationEnum.Left_By90);
						break;
					case OrientationEnum.Left_By90:
						directional.FaceDirection(OrientationEnum.Down_By180);
						break;
					case OrientationEnum.Down_By180:
						directional.FaceDirection(OrientationEnum.Right_By270);
						break;
				}
			}
		}

		#endregion
	}
}
