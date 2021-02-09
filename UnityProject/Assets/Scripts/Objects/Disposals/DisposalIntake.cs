using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Directionals;
using Systems.Disposals;
using AddressableReferences;

namespace Objects.Disposals
{
	public class DisposalIntake : DisposalMachine, IServerDespawn, IExaminable
	{
		private const float ANIMATION_TIME = 1.2f; // As per sprite sheet JSON file.
		private const float FLUSH_DELAY = 1;

		[SerializeField]
		private AddressableAudioSource disposalFlushSound = null;

		private DirectionalPassable directionalPassable;
		private DisposalVirtualContainer virtualContainer;

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

			if (TryGetComponent<Directional>(out var directional))
			{
				directional.OnDirectionChange.AddListener(OnDirectionChanged);
			}
			directionalPassable = GetComponent<DirectionalPassable>();
			DenyEntry();
		}

		private void Start()
		{
			UpdateSpriteOrientation();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (virtualContainer != null)
			{
				Despawn.ServerSingle(virtualContainer.gameObject);
			}
		}

		#endregion Lifecycle

		// Woman! I can hardly express
		// My mixed motions at my thoughtlessness
		// TODO: Don't poll, find some sort of trigger for when an entity enters the same tile.
		void UpdateMe()
		{
			if (MachineSecured == false || IsOperating) return;
			GatherEntities();
		}

		private void OnDirectionChanged(Orientation newDir)
		{
			UpdateSpriteOrientation();
		}

		private void SetIntakeOperating(bool isOperating)
		{
			IsOperating = isOperating;
			UpdateSpriteState();
		}

		#region Sprites

		private void UpdateSpriteState()
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

		private void UpdateSpriteOrientation()
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
			if (FloorPlatingExposed())
			{
				baseString = base.Examine().TrimEnd('.') + " and";
			}

			if (IsOperating)
			{
				return $"{baseString} is currently flushing its contents.";
			}
			else
			{
				return $"{baseString} is {(MachineSecured ? "ready" : "not ready")} for use.";
			}
		}

		#endregion Interactions

		private void GatherEntities()
		{
			var items = registerObject.Matrix.Get<ObjectBehaviour>(registerObject.LocalPosition, ObjectType.Item, true);
			var objects = registerObject.Matrix.Get<ObjectBehaviour>(registerObject.LocalPosition, ObjectType.Object, true);
			var players = registerObject.Matrix.Get<ObjectBehaviour>(registerObject.LocalPosition, ObjectType.Player, true);

			var filteredObjects = objects.ToList();
			filteredObjects.RemoveAll(entity =>
					// Only want to transport movable objects
					entity.GetComponent<ObjectBehaviour>().IsPushable == false ||
					// Don't add the virtual container to itself.
					entity.TryGetComponent<DisposalVirtualContainer>(out _)
			);

			if (items.Count() > 0 || filteredObjects.Count > 0 || players.Count() > 0)
			{
				StartCoroutine(RunIntakeSequence());
				virtualContainer.AddItems(items);
				virtualContainer.AddObjects(filteredObjects);
				virtualContainer.AddPlayers(players);
			}
		}

		private IEnumerator RunIntakeSequence()
		{
			// Intake orifice closes...
			SetIntakeOperating(true);
			DenyEntry();
			virtualContainer = SpawnNewContainer();
			yield return WaitFor.Seconds(FLUSH_DELAY);

			// Intake orifice closed. Release the charge.
			SoundManager.PlayNetworkedAtPos(disposalFlushSound, registerObject.WorldPositionServer, sourceObj: gameObject);
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

		private void DenyEntry()
		{
			directionalPassable.DenyPassableOnAllSides(PassType.Entering);
		}

		private void AllowEntry()
		{
			directionalPassable.AllowPassableAtSetSides(PassType.Entering);
		}
	}
}
