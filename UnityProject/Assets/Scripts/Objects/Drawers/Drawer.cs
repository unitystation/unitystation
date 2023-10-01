using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using AddressableReferences;
using Logs;
using Messages.Server.SoundMessages;
using Random = UnityEngine.Random;

namespace Objects.Drawers
{
	/// <summary>
	/// A generic drawer component designed for multi-tile drawer objects.
	/// </summary>
	[ExecuteInEditMode]
	public class Drawer : NetworkBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>, IEscapable
	{
	[SerializeField] private AddressableAudioSource BinOpenSFX = null;
	[SerializeField] private AddressableAudioSource BinCloseSFX = null;
	/// <summary>
	/// How long does it take before players can escape from this drawer? (Put it to 0 to disable it)
	/// </summary>
	[SerializeField] protected float escapeTime = 8f;

		protected enum DrawerState
		{
			Open = 0,
			Shut = 1
		}

		protected enum SpriteOrientation
		{
			South = 0,
			North = 1,
			East = 2,
			West = 3
		}

		protected RegisterObject registerObject;
		protected Rotatable rotatable;
		protected UniversalObjectPhysics drawerPushPull;
		protected SpriteHandler drawerSpriteHandler;

		protected Matrix Matrix => registerObject.Matrix;
		protected Vector3Int DrawerWorldPosition => registerObject.WorldPosition;
		// This script uses Matrix.Get(), which requires a local position, but Spawn requires a world position.
		protected Vector3Int TrayWorldPosition => GetTrayPosition(DrawerWorldPosition); // Spawn requires world position
		protected Vector3Int TrayLocalPosition => ((Vector3)TrayWorldPosition).ToLocalInt(Matrix);

		protected GameObject tray;
		protected UniversalObjectPhysics ObjectPhysics;
		protected UniversalObjectPhysics trayBehaviour;
		protected ObjectContainer container;
		protected SpriteHandler traySpriteHandler;

		[SerializeField]
		[Tooltip("The corresponding tray that the drawer will spawn.")]
		protected GameObject trayPrefab = default;
		[SerializeField]
		[Tooltip("Whether the drawer can store players.")]
		protected bool storePlayers = true;

		protected DrawerState drawerState = DrawerState.Shut;

		#region Lifecycle

		protected virtual void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			rotatable = GetComponent<Rotatable>();
			drawerPushPull = GetComponent<UniversalObjectPhysics>();
			container = GetComponent<ObjectContainer>();
			drawerSpriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			registerObject = GetComponent<RegisterObject>();
			ServerInit();
			rotatable.OnRotationChange.AddListener(OnDirectionChanged);
		}

		private void OnDisable()
		{
			rotatable.OnRotationChange.RemoveListener(OnDirectionChanged);
		}

		private void ServerInit()
		{
			SpawnResult traySpawn = Spawn.ServerPrefab(trayPrefab, DrawerWorldPosition);
			if (!traySpawn.Successful)
			{
				Loggy.LogError($"Failed to spawn tray! Is {name} prefab missing reference to {nameof(traySpawn)} prefab?",
					Category.Machines);
				return;
			}
			tray = traySpawn.GameObject;

			tray.GetComponent<InteractableDrawerTray>().parentDrawer = this;
			traySpriteHandler = tray.GetComponentInChildren<SpriteHandler>();
			ObjectPhysics = tray.GetComponent<UniversalObjectPhysics>();
			trayBehaviour = ObjectPhysics;
			trayBehaviour.StoreTo(container);

			UpdateSpriteState();
			UpdateSpriteOrientation();
		}

		#endregion

		/// <summary>
		/// If the drawer is about to despawn, despawn the tray too, so it is not stranded at HiddenPos.
		/// </summary>
		public void OnDespawnServer(DespawnInfo despawnInfo)
		{
			if (drawerState == DrawerState.Open) return;

			_ = Despawn.ServerSingle(tray);
		}

		#region Sprite

		private void OnDirectionChanged(OrientationEnum newDirection)
		{
			UpdateSpriteOrientation();
		}

		public void OnEditorDirectionChange()
		{
			UpdateSpriteOrientation();
		}

		/// <summary>
		/// Updates the drawer to the given state and sets the sprites accordingly.
		/// </summary>
		/// <param name="newState">The new state the drawer should be set to</param>
		protected void SetDrawerState(DrawerState newState)
		{
			drawerState = newState;
			UpdateSpriteState();
		}

		private void UpdateSpriteState()
		{
			drawerSpriteHandler.ChangeSprite((int)drawerState);
		}

		private void UpdateSpriteOrientation()
		{
			int spriteVariant = (int)GetSpriteDirection();
			drawerSpriteHandler.ChangeSpriteVariant(spriteVariant);

			if (traySpriteHandler != null)
			{
				traySpriteHandler.ChangeSpriteVariant(spriteVariant);
			}
		}

		private SpriteOrientation GetSpriteDirection()
		{
			switch (rotatable.CurrentDirection)
			{
				case OrientationEnum.Up_By0: return SpriteOrientation.North;
				case OrientationEnum.Down_By180: return SpriteOrientation.South;
				case OrientationEnum.Left_By90: return SpriteOrientation.West;
				case OrientationEnum.Right_By270: return SpriteOrientation.East;
				default: return SpriteOrientation.South;
			}
		}

		#endregion Sprite

		#region Interactions

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject != null) return false;

			return true;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (drawerState == DrawerState.Open) CloseDrawer();
			else OpenDrawer();
		}

		#endregion Interactions

		/// <summary>
		/// Returns the tray position from the current orientation and the given drawer position.
		/// </summary>
		/// <param name="drawerPosition">The drawer position</param>
		/// <returns>The tray position</returns>
		protected Vector3Int GetTrayPosition(Vector3Int drawerPosition)
		{
			return (drawerPosition + rotatable.CurrentDirection.ToLocalVector3()).CutToInt();
		}

		#region Server Only

		public virtual void OpenDrawer()
		{
			if(drawerState == DrawerState.Open) return;
			trayBehaviour.StoreTo(null);
			ObjectPhysics.AppearAtWorldPositionServer(TrayWorldPosition);

			container.RetrieveObjects(TrayWorldPosition);

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(BinOpenSFX, DrawerWorldPosition, audioSourceParameters, sourceObj: gameObject);
			SetDrawerState(DrawerState.Open);
		}

		public virtual void CloseDrawer()
		{
			trayBehaviour.StoreTo(container);
			GatherObjects();
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(BinCloseSFX, DrawerWorldPosition, audioSourceParameters, sourceObj: gameObject);
			SetDrawerState(DrawerState.Shut);
		}

		protected virtual void GatherObjects()
		{
			var items = Matrix.Get<UniversalObjectPhysics>(TrayLocalPosition, true);
			foreach (var item in items)
			{
				//Prevents stuff like cameras ending up inside (check for health in case player wearing mag boots)
				if(item.IsNotPushable && item.TryGetComponent<HealthV2.LivingHealthMasterBase>(out _) == false) continue;

				if (storePlayers == false && item.TryGetComponent<PlayerScript>(out _)) continue;

				// Other position fields such as registerObject.WorldPosition seem to give tile integers.
				var tileOffsetPosition = item.transform.position - TrayWorldPosition;
				container.StoreObject(item.gameObject, tileOffsetPosition);
			}
		}

		public void EntityTryEscape(GameObject entity,Action ifCompleted, MoveAction moveAction)
		{
			if(entity.Player() == null) return;
			if (escapeTime <= 0.1f)
			{
				OpenDrawer();
				return;
			}
			var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Escape,
				true, false, true, true), OpenDrawer);
			bar.ServerStartProgress(gameObject.RegisterTile(), escapeTime, entity);
			Chat.AddActionMsgToChat(entity,
				$"You begin breaking out of the {gameObject.ExpensiveName()}...",
				$"You hear noises coming from the {gameObject.ExpensiveName()}... Something must be trying to break out!");
		}

		#endregion Server Only
	}
}
