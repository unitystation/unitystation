using System;
using System.Collections;
using UnityEngine;
using Core.Directionals;
using Systems.Disposals;
using AddressableReferences;

namespace Objects.Disposals
{
	public class DisposalIntake : DisposalMachine, IExaminable
	{
		private const float ANIMATION_TIME = 1.2f; // As per sprite sheet JSON file.
		private const float FLUSH_DELAY = 1;

		[SerializeField]
		private AddressableAudioSource disposalFlushSound = null;

		private DirectionalPassable directionalPassable;

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
			directionalPassable = GetComponent<DirectionalPassable>();
			DenyEntry();
		}

		private void OnEnable()
		{
			if (TryGetComponent<Rotatable>(out var rotatable))
			{
				rotatable.OnRotationChange.AddListener(OnDirectionChanged);
			}
		}

		private void Start()
		{
			UpdateSpriteOrientation();
		}

		#endregion Lifecycle

		// Woman! I can hardly express
		// My mixed motions at my thoughtlessness
		// TODO: Don't poll, find some sort of trigger for when an entity enters the same tile.
		private void UpdateMe()
		{
			if (MachineSecured == false || IsOperating) return;
			GatherEntities();
		}

		private void OnDirectionChanged(OrientationEnum newDir)
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
			switch (directionalPassable.RotatableChecked.Component.CurrentDirection)
			{
				case OrientationEnum.Up_By0:
					baseSpriteHandler.ChangeSpriteVariant(1);
					break;
				case OrientationEnum.Down_By180:
					baseSpriteHandler.ChangeSpriteVariant(0);
					break;
				case OrientationEnum.Left_By90:
					baseSpriteHandler.ChangeSpriteVariant(3);
					break;
				case OrientationEnum.Right_By270:
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
			objectContainer.GatherObjects();

			if (objectContainer.IsEmpty == false)
			{
				StartCoroutine(RunIntakeSequence());
			}
		}

		private IEnumerator RunIntakeSequence()
		{
			// Intake orifice closes...
			SetIntakeOperating(true);
			DenyEntry();
			yield return WaitFor.Seconds(FLUSH_DELAY);

			// Intake orifice closed. Release the charge.
			SoundManager.PlayNetworkedAtPos(disposalFlushSound, registerObject.WorldPositionServer, sourceObj: gameObject);
			DisposalsManager.Instance.NewDisposal(gameObject);

			// Restore charge, open orifice.
			yield return WaitFor.Seconds(ANIMATION_TIME - FLUSH_DELAY);
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

			if (TryGetComponent<Rotatable>(out var rotatable))
			{
				rotatable.OnRotationChange.RemoveListener(OnDirectionChanged);
			}
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
