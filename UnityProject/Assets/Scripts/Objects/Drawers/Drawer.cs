using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using AddressableReferences;
using Messages.Server.SoundMessages;

namespace Objects.Drawers
{
	/// <summary>
	/// A generic drawer component designed for multi-tile drawer objects.
	/// </summary>
	[RequireComponent(typeof(ObjectBehaviour))] // For setting held items' containers to the drawer.
	[ExecuteInEditMode]
	public class Drawer : NetworkBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>, IEscapable
	{
	[SerializeField] private AddressableAudioSource BinOpenSFX = null;
	[SerializeField] private AddressableAudioSource BinCloseSFX = null;

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
		protected Directional directional;
		protected PushPull drawerPushPull;
		protected SpriteHandler drawerSpriteHandler;

		protected Matrix Matrix => registerObject.Matrix;
		protected Vector3Int DrawerWorldPosition => registerObject.WorldPosition;
		// This script uses Matrix.Get(), which requires a local position, but Spawn requires a world position.
		protected Vector3Int TrayWorldPosition => GetTrayPosition(DrawerWorldPosition); // Spawn requires world position
		protected Vector3Int TrayLocalPosition => ((Vector3)TrayWorldPosition).ToLocalInt(Matrix);

		protected GameObject tray;
		protected CustomNetTransform trayTransform;
		protected ObjectBehaviour trayBehaviour;
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
			directional = GetComponent<Directional>();
			drawerPushPull = GetComponent<PushPull>();
			container = GetComponent<ObjectContainer>();
			drawerSpriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			registerObject = GetComponent<RegisterObject>();
			ServerInit();
			directional.OnDirectionChange.AddListener(OnDirectionChanged);
		}

		private void ServerInit()
		{
			SpawnResult traySpawn = Spawn.ServerPrefab(trayPrefab, DrawerWorldPosition);
			if (!traySpawn.Successful)
			{
				Logger.LogError($"Failed to spawn tray! Is {name} prefab missing reference to {nameof(traySpawn)} prefab?",
					Category.Machines);
				return;
			}
			tray = traySpawn.GameObject;

			tray.GetComponent<InteractableDrawerTray>().parentDrawer = this;
			traySpriteHandler = tray.GetComponentInChildren<SpriteHandler>();
			trayTransform = tray.GetComponent<CustomNetTransform>();
			trayBehaviour = tray.GetComponent<ObjectBehaviour>();
			trayBehaviour.parentContainer = drawerPushPull;
			trayBehaviour.VisibleState = false;

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

		private void OnDirectionChanged(Orientation newDirection)
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
			switch (directional.CurrentDirection.AsEnum())
			{
				case OrientationEnum.Up: return SpriteOrientation.North;
				case OrientationEnum.Down: return SpriteOrientation.South;
				case OrientationEnum.Left: return SpriteOrientation.West;
				case OrientationEnum.Right: return SpriteOrientation.East;
				default: return SpriteOrientation.South;
			}
		}

		#endregion Sprite

		#region Interactions

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
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
			return (drawerPosition + directional.CurrentDirection.Vector).CutToInt();
		}

		#region Server Only

		public virtual void OpenDrawer()
		{
			trayBehaviour.parentContainer = null;
			trayTransform.SetPosition(TrayWorldPosition);

			container.RetrieveObjects(TrayWorldPosition);

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(BinOpenSFX, DrawerWorldPosition, audioSourceParameters, sourceObj: gameObject);
			SetDrawerState(DrawerState.Open);
		}

		public virtual void CloseDrawer()
		{
			trayBehaviour.parentContainer = drawerPushPull;
			trayBehaviour.VisibleState = false;

			GatherObjects();
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(BinCloseSFX, DrawerWorldPosition, audioSourceParameters, sourceObj: gameObject);
			SetDrawerState(DrawerState.Shut);
		}

		protected virtual void GatherObjects()
		{
			var items = Matrix.Get<ObjectBehaviour>(TrayLocalPosition, true);
			foreach (ObjectBehaviour item in items)
			{
				if (storePlayers == false && item.TryGetComponent<PlayerScript>(out _)) continue;

				// Other position fields such as registerObject.WorldPosition seem to give tile integers.
				var tileOffsetPosition = item.transform.position - TrayWorldPosition;
				container.StoreObject(item.gameObject, tileOffsetPosition);
			}
		}

		public void EntityTryEscape(GameObject entity)
		{
			if (entity.Player() != null)
			{
				OpenDrawer();
			}
		}

		#endregion Server Only
	}
}
