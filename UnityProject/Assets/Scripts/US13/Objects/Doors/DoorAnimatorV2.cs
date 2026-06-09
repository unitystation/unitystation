using System;
using Cysharp.Threading.Tasks;
using Mirror;
using NaughtyAttributes;
using SecureStuff;
using UnityEngine;
using US13.Core.Sprite_Handler;

namespace US13.Objects.Doors
{
	public class DoorAnimatorV2 : NetworkBehaviour
	{
		#region Inspector
		[SerializeField, BoxGroup("Sprite Layers")]
		[Tooltip("Game object which represents the base of this door")]
		private GameObject doorBase = null;
		public GameObject DoorBase => doorBase;

		[SerializeField, BoxGroup("Sprite Layers")]
		[Tooltip("Game object which represents the light layer of this door")]
		private GameObject overlaySparks = null;
		public GameObject OverlaySparks => overlaySparks;

		[SerializeField, BoxGroup("Sprite Layers")]
		[Tooltip("Game object which represents the light layer of this door")]
		private GameObject overlayLights = null;
		public GameObject OverlayLights => overlayLights;

		[SerializeField, BoxGroup("Sprite Layers")]
		[Tooltip("Game object which represents the fill layer of this door")]
		private GameObject overlayFill = null;
		public GameObject OverlayFill => overlayFill;

		[SerializeField, BoxGroup("Sprite Layers")]
		[Tooltip("Game object which represents the welded and effects layer for this door")]
		private GameObject overlayWeld = null;
		public GameObject OverlayWeld => overlayWeld;

		[SerializeField, BoxGroup("Sprite Layers")]
		[Tooltip("Game object which represents the hacking panel layer for this door")]
		private GameObject overlayHacking = null;
		public GameObject OverlayHacking => overlayHacking;

		[SerializeField]
		[Tooltip("Time this door's opening animation takes")]
		private float openingAnimationTime = 0.6f;

		[SerializeField]
		[Tooltip("Time this door's closing animation takes")]
		private float closingAnimationTime = 0.6f;

		[SerializeField]
		[Tooltip("Time this door's denied animation takes")]
		private float deniedAnimationTime = 0.6f;

		[SerializeField]
		[Tooltip("Time this door's warning animation takes")]
		private float warningAnimationTime = 0.6f;

		[SerializeField]
		[Tooltip("How fraction of the door needs to be to be open to allow the player to slip through? (0 is none, 1 is fully open)")]
		private float slipThroughScaler = 0.5f;
		#endregion

#region Initialization
		[PlayModeOnly] [SyncVar(hook = nameof(SyncDoorStatus))] public DoorUpdateType SyncDoorUpdateType;
		[SyncVar] public bool PanelOpen;
		[PlayModeOnly] [SyncVar] public bool LightsWork;

		public event Action AnimationStarted;
		public event Action AnimationFinished;
		public event Action AnimationClosed;
		public event Action AnimationOpened;


		private SpriteHandler doorBaseHandler;
		private SpriteHandler overlaySparksHandler;
		private SpriteHandler overlayLightsHandler;
		private SpriteHandler overlayFillHandler;
		private SpriteHandler overlayWeldHandler;
		private SpriteHandler overlayHackingHandler;
		private SpriteRenderer spriteRenderer;

		private int openMaskingLayer;
		private int closedMaskingLayer;
		private int openSortingLayer;
		private int closedSortingLayer;

		private int previousLightSprite = -1;
		private DoorMasterController doorMasterController;

		private void Awake()
		{
			doorBaseHandler = doorBase.GetComponent<SpriteHandler>();
			overlaySparksHandler = overlaySparks.GetComponent<SpriteHandler>();
			overlayLightsHandler = overlayLights.GetComponent<SpriteHandler>();
			overlayFillHandler = overlayFill.GetComponent<SpriteHandler>();
			overlayWeldHandler = overlayWeld.GetComponent<SpriteHandler>();
			overlayHackingHandler = overlayHacking.GetComponent<SpriteHandler>();
			doorMasterController = this.GetComponent<DoorMasterController>();
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();

			PanelOpen = false;
			LightsWork = true;

			SetLayerData();
		}

		/// <summary>
		/// Sets the appropriate sorting and masking layer for the door
		/// </summary>
		public void SetLayerData()
		{
			openMaskingLayer = LayerMask.NameToLayer("Door Open");

			// Windowed doors uses the windowed masking layer when closed
			if (doorMasterController.isWindowedDoor == true)
				closedMaskingLayer = LayerMask.NameToLayer("Windows");
			else
				closedMaskingLayer = LayerMask.NameToLayer("Door Closed");

			//If this is a firelock it goes on top of other doors when closed
			if (doorMasterController.IsFireLock)
			{
				closedSortingLayer = SortingLayer.NameToID("WallObject");
				openSortingLayer = SortingLayer.NameToID("Machines");
			}
			else
			{
				closedSortingLayer = SortingLayer.NameToID("Doors Closed");
				openSortingLayer = SortingLayer.NameToID("Doors Open");
			}
		}

#endregion

		private async UniTaskVoid ChangeLayers(bool isClosing)
		{
			if (isClosing)
			{
				spriteRenderer.sortingLayerID = closedSortingLayer;
				doorMasterController.RegisterTile.SetNewSortingLayer(closedSortingLayer);
			}
			else
			{
				spriteRenderer.sortingLayerID = openSortingLayer;
				doorMasterController.RegisterTile.SetNewSortingLayer(openSortingLayer);
			}
		}

		/// <summary>
		/// Sets the masking layer of the door
		/// </summary>
		private void SetMaskingLayer(int layer)
		{
			// TODO: There is a nice visual effect if we don't do this for airlocks that are entered from North/South
			// But East/West doors will occlude all vision, same with shutters from N/S. We could consider changing
			// how doors interact with vision, but because of handling perspective angles it would be rather complicated
			gameObject.layer = layer;
			foreach (Transform child in transform)
			{
				child.gameObject.layer = layer;
			}
		}

		public enum DoorUpdateType
		{
			Open = 0,
			Close = 1,
			AccessDenied = 2,
			PressureWarn = 3
		}

		public void SyncDoorStatus(DoorUpdateType old, DoorUpdateType newv)
		{
			SyncDoorUpdateType = newv;
			if (old != newv)
			{
				PlayAnimation(newv);
			}
		}

		public void PlayAnimation(DoorUpdateType type, bool skipAnimation = false)
		{
			if (doorMasterController == null) return;

			if (type == DoorUpdateType.Open)
				PlayOpeningAnimation(skipAnimation, PanelOpen, LightsWork).Forget();

			else if (type == DoorUpdateType.Close)
				PlayClosingAnimation(skipAnimation, PanelOpen, LightsWork).Forget();

			else if (type == DoorUpdateType.AccessDenied)
				PlayDeniedAnimation().Forget();

			else if (type == DoorUpdateType.PressureWarn)
				PlayPressureWarningAnimation().Forget();
		}

		public async UniTaskVoid PlayOpeningAnimation(bool skipAnimation = false, bool panel = false, bool lights = true)
		{
			LightsWork = lights;
			PanelOpen = panel;
			AnimationStarted?.Invoke();

			if (skipAnimation == false)
			{
				ToggleOpeningClosing(DoorFrame.Opening, Panel.Opening, Lights.Opening);

				//Simulates being able slipping through door as its opening
				if (slipThroughScaler > 0 && slipThroughScaler < 1)
				{
					await UniTask.WaitForSeconds(openingAnimationTime * slipThroughScaler);
					SetMaskingLayer(openMaskingLayer);
					AnimationOpened?.Invoke();
					await UniTask.WaitForSeconds(openingAnimationTime * (1 - slipThroughScaler));
				}
				else
				{
					AnimationOpened?.Invoke();
					await UniTask.WaitForSeconds(openingAnimationTime);
				}

			}
			else
			{
				AnimationOpened?.Invoke();
			}

			//Toggle layers after animation to hide firelocks behind door
			ChangeLayers(isClosing: false).Forget();

			ToggleDoorOpenClosed();

			AnimationFinished?.Invoke();
		}

		public async UniTaskVoid PlayClosingAnimation(bool skipAnimation = false, bool panel = false, bool lights = true)
		{
			LightsWork = lights;
			PanelOpen = panel;
			AnimationStarted?.Invoke();

			//Toggle layers before animation to show firelock close over door
			ChangeLayers(isClosing: true).Forget();

			if (skipAnimation == false)
			{
				ToggleOpeningClosing(DoorFrame.Closing, Panel.Closing, Lights.Closing);

				//Simulates being able slipping through door as its closing
				if (slipThroughScaler > 0 && slipThroughScaler < 1)
				{
					await UniTask.WaitForSeconds(openingAnimationTime * (1 - slipThroughScaler));
					SetMaskingLayer(closedMaskingLayer);
					AnimationClosed?.Invoke();
					await UniTask.WaitForSeconds(openingAnimationTime * slipThroughScaler);
				}
				else
				{
					AnimationClosed?.Invoke();
					await UniTask.WaitForSeconds(openingAnimationTime);
				}
			}
			else
			{
				AnimationClosed?.Invoke();
			}

			ToggleDoorOpenClosed();

			AnimationFinished?.Invoke();
		}

		private void ToggleOpeningClosing(DoorFrame action, Panel panel, Lights lights)
		{
			if (PanelOpen)
				overlayHackingHandler.SetCatalogueIndexSprite((int)panel);

			if (LightsWork)
			{
				overlayLightsHandler.SetCatalogueIndexSprite((int)lights);
				previousLightSprite = (int)lights;
			}

			overlayFillHandler.SetCatalogueIndexSprite((int)action);
			doorBaseHandler.SetCatalogueIndexSprite((int)action);
		}

		private void ToggleDoorOpenClosed()
		{
			if (doorMasterController.IsClosed)
			{
				//Change to closed sprite after it is done closing
				if (PanelOpen) overlayHackingHandler.SetCatalogueIndexSprite((int)Panel.Closed);
				else overlayHackingHandler.SetCatalogueIndexSprite((int)Panel.NoPanel);

				overlayFillHandler.SetCatalogueIndexSprite((int)DoorFrame.Closed);
				doorBaseHandler.SetCatalogueIndexSprite((int)DoorFrame.Closed);
			}
			else
			{
				if (PanelOpen) overlayHackingHandler.SetCatalogueIndexSprite((int)Panel.Open);
				else overlayHackingHandler.SetCatalogueIndexSprite((int)Panel.NoPanel);

				overlayFillHandler.SetCatalogueIndexSprite((int)DoorFrame.Open);
				doorBaseHandler.SetCatalogueIndexSprite((int)DoorFrame.Open);
			}

			overlayLightsHandler.SetCatalogueIndexSprite((int)Lights.NoLight);
			previousLightSprite = (int)Lights.NoLight;
		}

		public async UniTaskVoid PlayDeniedAnimation()
		{
			AnimationStarted?.Invoke();

			if (previousLightSprite == -1)
			{
				previousLightSprite = overlayLightsHandler.CurrentSpriteIndex;
			}
			overlayLightsHandler.SetCatalogueIndexSprite((int)Lights.Denied);
			await UniTask.WaitForSeconds(deniedAnimationTime);

			if (previousLightSprite == -1) previousLightSprite = 0;
			overlayLightsHandler.SetCatalogueIndexSprite(previousLightSprite);
			previousLightSprite = -1;
			AnimationFinished?.Invoke();
		}

		public async UniTaskVoid PlayPressureWarningAnimation()
		{
			AnimationStarted?.Invoke();

			if (previousLightSprite == -1)
			{
				previousLightSprite = overlayLightsHandler.CurrentSpriteIndex;
			}
			overlayLightsHandler.SetCatalogueIndexSprite((int)Lights.PressureWarning);
			await UniTask.WaitForSeconds(warningAnimationTime);

			if (previousLightSprite == -1) previousLightSprite = 0;
			overlayLightsHandler.SetCatalogueIndexSprite(previousLightSprite);
			previousLightSprite = -1;
			AnimationFinished?.Invoke();
		}

		public void TurnOffAllLights()
		{
			overlayLightsHandler.SetCatalogueIndexSprite((int)Lights.NoLight);
			previousLightSprite = (int)Lights.NoLight;
		}

		public void TurnOnBoltsLight()
		{
			overlayLightsHandler.SetCatalogueIndexSprite((int)Lights.BoltsLights);
			previousLightSprite = (int)Lights.BoltsLights;
		}

		public void AddWeldOverlay()
		{
			overlayWeldHandler.SetCatalogueIndexSprite((int)Weld.Weld);
		}

		public void RemoveWeldOverlay()
		{
			overlayWeldHandler.SetCatalogueIndexSprite((int)Weld.NoWeld);
		}

		public void AddPanelOverlay()
		{
			PanelOpen = true;
			overlayHackingHandler.SetCatalogueIndexSprite((int)Panel.Closed);
		}

		public void RemovePanelOverlay()
		{
			PanelOpen = false;
			overlayHackingHandler.SetCatalogueIndexSprite((int)Panel.NoPanel);
		}

		public void ForceSpriteSync()
		{
			ToggleDoorOpenClosed();
			SetMaskingLayer(doorMasterController.IsClosed ? closedMaskingLayer : openMaskingLayer);
			ChangeLayers(doorMasterController.IsClosed).Forget();
		}
	}

	public enum Weld
	{
		NoWeld,
		Weld
	}

	public enum DoorFrame
	{
		Closed,
		Opening,
		Open,
		Closing
	}

	public enum Lights
	{
		NoLight,
		Denied,
		Opening,
		Closing,
		Emergency,
		BoltsLights,
		PressureWarning
	}

	public enum Sparks
	{
		NoSparks,
		SparkLights,
		BrokenSparks,
		DamagedSparks,
		OpenSparks
	}

	public enum Panel
	{
		NoPanel,
		Closed,
		Opening,
		Open,
		Closing
	}
}
