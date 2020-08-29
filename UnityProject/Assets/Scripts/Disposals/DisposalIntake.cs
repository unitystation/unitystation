using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Disposals
{
	public class DisposalIntake : DisposalMachine, IServerDespawn, IExaminable
	{
		const float ANIMATION_TIME = 1.2f; // As per sprite sheet JSON file.
		const float FLUSH_DELAY = 1;

		DirectionalPassable directionalPassable;
		DisposalVirtualContainer virtualContainer;

		public bool IsOperating { get; private set; }

		private enum SpriteState
		{
			Idle = 0,
			Operating = 1
		}

		#region Lifecycle

		protected override void Awake()
		{
			base.Awake();

			if (TryGetComponent(out Directional directional))
			{
				directional.OnDirectionChange.AddListener(OnDirectionChanged);
			}
			directionalPassable = GetComponent<DirectionalPassable>();
			DenyEntry();
		}

		void Start()
		{
			UpdateSpriteOrientation();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (virtualContainer != null) Despawn.ServerSingle(virtualContainer.gameObject);
		}

		#endregion Lifecycle

		// Woman! I can hardly express
		// My mixed motions at my thoughtlessness
		// TODO: Don't poll, find some sort of trigger for when an entity enters the same tile.
		void UpdateMe()
		{
			if (!MachineSecured || IsOperating) return;
			GatherEntities();
		}

		private void OnDirectionChanged(Orientation newDir)
		{
			UpdateSpriteOrientation();
		}

		void SetIntakeOperating(bool isOperating)
		{
			IsOperating = isOperating;
			UpdateSpriteState();
		}

		#region Sprites

		void UpdateSpriteState()
		{
			if (IsOperating)
			{
				baseSpriteHandler.ChangeSprite((int) SpriteState.Operating);
			}
			else
			{
				baseSpriteHandler.ChangeSprite((int) SpriteState.Idle);
			}
		}

		void UpdateSpriteOrientation()
		{
			switch (directionalPassable.Directional.CurrentDirection.AsEnum())
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

			if (IsOperating) return $"{baseString} is currently flushing its contents.";
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
			SetIntakeOperating(true);
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
			SetIntakeOperating(false);
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
