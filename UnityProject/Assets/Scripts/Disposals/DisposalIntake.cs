using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

namespace Disposals
{
	public class DisposalIntake : DisposalMachine, IServerDespawn, IExaminable
	{
		const float ANIMATION_TIME = 1.2f; // As per sprite sheet JSON file.
		const float FLUSH_DELAY = 1;

		DirectionalPassable directionalPassable;

		[SyncVar(hook = nameof(OnSyncIntakeState))]
		bool intakeOperating = false;
		[SyncVar(hook = nameof(OnSyncOrientation))]
		OrientationEnum orientation;

		Coroutine intakeSequence;
		DisposalVirtualContainer virtualContainer;

		public bool IntakeOperating => intakeOperating;

		#region Initialisation

		protected override void Awake()
		{
			base.Awake();

			if (TryGetComponent(out Directional directional))
			{
				orientation = directional.InitialDirection;
				directional.OnDirectionChange.AddListener(OnDirectionChanged);
			}
			directionalPassable = GetComponent<DirectionalPassable>();
			DenyEntry();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			UpdateSpriteOutletState();
			UpdateSpriteOrientation();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (virtualContainer != null) Despawn.ServerSingle(virtualContainer.gameObject);
		}

		#endregion Initialisation

		// Woman! I can hardly express
		// My mixed motions at my thoughtlessness
		// TODO: Don't poll, find some sort of trigger for when an entity enters the same tile.
		void UpdateMe()
		{
			if (!MachineSecured || IntakeOperating) return;
			GatherEntities();
		}

		private void OnDirectionChanged(Orientation newDir)
		{
			orientation = newDir.AsEnum();
		}

		#region Sync

		void OnSyncIntakeState(bool oldState, bool newState)
		{
			intakeOperating = newState;
			UpdateSpriteOutletState();
		}

		void OnSyncOrientation(OrientationEnum oldState, OrientationEnum newState)
		{
			orientation = newState;
			UpdateSpriteOrientation();
		}

		#endregion Sync

		#region Sprites

		void UpdateSpriteOutletState()
		{
			if (IntakeOperating) baseSpriteHandler.ChangeSprite(1);
			else baseSpriteHandler.ChangeSprite(0);
		}

		void UpdateSpriteOrientation()
		{
			switch (orientation)
			{
				case OrientationEnum.Up:
					baseSpriteHandler.ChangeSpriteVariant(1);
					break;
				case OrientationEnum.Down:
					baseSpriteHandler.ChangeSpriteVariant(0);
					break;
				case OrientationEnum.Left:
					baseSpriteHandler.ChangeSpriteVariant(3);
					break;
				case OrientationEnum.Right:
					baseSpriteHandler.ChangeSpriteVariant(2);
					break;
			}
		}

		#endregion Sprites

		#region Interactions

		public override string Examine(Vector3 worldPos = default)
		{
			string baseString = "It";
			if (FloorPlatingExposed()) baseString = base.Examine().TrimEnd('.') + " and";

			if (IntakeOperating) return $"{baseString} is currently flushing its contents.";
			else return $"{baseString} is ready for use.";
		}

		#endregion Interactions

		void GatherEntities()
		{
			var items = registerObject.Matrix.Get<ObjectBehaviour>(registerObject.LocalPosition, ObjectType.Item, true);
			var objects = registerObject.Matrix.Get<ObjectBehaviour>(registerObject.LocalPosition, ObjectType.Object, true);
			var players = registerObject.Matrix.Get<ObjectBehaviour>(registerObject.LocalPosition, ObjectType.Player, true);

			var filteredObjects = objects.ToList();
			filteredObjects.RemoveAll(entity =>
					// Only want to transport movable objects
					!entity.GetComponent<ObjectBehaviour>().IsPushable ||
					// Don't add the virtual container to itself.
					entity.TryGetComponent(out DisposalVirtualContainer container)
			);

			if (items.Count() > 0 || filteredObjects.Count > 0 || players.Count() > 0)
			{
				StartCoroutine(RunIntakeSequence());
				virtualContainer.AddItems(items);
				virtualContainer.AddObjects(filteredObjects);
				virtualContainer.AddPlayers(players);
			}
		}

		IEnumerator RunIntakeSequence()
		{
			// Intake orifice closes...
			intakeOperating = true;
			DenyEntry();
			virtualContainer = SpawnNewContainer();
			yield return WaitFor.Seconds(FLUSH_DELAY);

			// Intake orifice closed. Release the charge.
			SoundManager.PlayNetworkedAtPos("DisposalMachineFlush", registerObject.WorldPositionServer, sourceObj: gameObject);
			DisposalsManager.Instance.NewDisposal(virtualContainer);

			// Restore charge, open orifice.
			yield return WaitFor.Seconds(ANIMATION_TIME - FLUSH_DELAY);
			virtualContainer = null;
			AllowEntry();
			intakeOperating = false;
		}

		#region Construction

		protected override void SetMachineInstalled()
		{
			base.SetMachineInstalled();
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			AllowEntry();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		protected override void SetMachineUninstalled()
		{
			base.SetMachineUninstalled();
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			DenyEntry();
		}

		#endregion Construction

		void DenyEntry()
		{
			// TODO: Figure out a way to exclude this object from directional passable checks
			// before we can re-enable this line, else we cannot move the object.
			//directionalPassable.DenyPassableOnAllSides();
		}

		void AllowEntry()
		{
			directionalPassable.AllowPassableAtSetSides();
		}
	}
}
