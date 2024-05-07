using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InGameGizmos;
using Logs;
using Newtonsoft.Json;
using Shared.Managers;
using TileManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EscapeKeyTarget))]
public class CopyAndPaste  : SingletonManager<CopyAndPaste>
{

	//TODO Remove mid placement gizmo

	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	public Button StopSelectingButton;
	public Button StopUnSelectingButton;


	public Button SelectingButton;
	public Button UnSelectingButton;

	public bool Updating = false;


	public List<GizmoAndBox> PositionsToCopy = new List<GizmoAndBox>();


	public List<GameGizmoSquare> PreviewGizmos = new List<GameGizmoSquare>();

	public Vector3 ActiveBoundStart;
	public Vector3 ActiveBoundCurrent;

	public GameGizmoSquare ActiveGizmo;
	private readonly Vector3 GIZMO_OFFSET = new Vector3(-0.5f, -0.5f, 0);

	public string Clipboard;

	public MouseGrabber MouseGrabberPrefab;

	public MouseGrabber ActiveMouseGrabber; //TODO Destroy?


	public MapSaver.MapSaver.MatrixData currentlyActivePaste;

	private void OnEnable()
	{
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
	}

	public struct GizmoAndBox
	{
		public BetterBounds BetterBounds;
		public GameGizmoSquare GameGizmoSquare;
	}

	public override void Start()
	{
		base.Start();
		//this.gameObject.SetActive(false);
	}


	private void OnDisable()
	{
		OnEscape();
		if (Updating)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			Updating = false;
		}
	}

	[NaughtyAttributes.Button]
	public void OnSelected()
	{
		UnSelectingButton.interactable = false;
		SelectingButton.interactable = false;
		StopSelectingButton.interactable = true;
		StopUnSelectingButton.interactable = false;

		UIManager.IsMouseInteractionDisabled = true;
		escapeKeyTarget.enabled = true;
		if (Updating == false)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			Updating = true;
		}
	}

	[NaughtyAttributes.Button]
	public void OnUnSelectedSelected()
	{
		UnSelectingButton.interactable = false;
		SelectingButton.interactable = false;
		StopSelectingButton.interactable = false;
		StopUnSelectingButton.interactable = true;


		UIManager.IsMouseInteractionDisabled = true;
		escapeKeyTarget.enabled = true;
		if (Updating == false)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			Updating = true;
		}
	}

	public void OnEscape()
	{
		//stop drawing
		if (Updating)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			Updating = false;
		}

		UIManager.IsMouseInteractionDisabled = false;
		if (escapeKeyTarget != null)
		{
			escapeKeyTarget.enabled = false;
		}

		StopSelectingButton.interactable = false;
		StopUnSelectingButton.interactable = false;
		UnSelectingButton.interactable = true;
		SelectingButton.interactable = true;
	}


	private void UpdateMe()
	{

		if (ActiveGizmo != null)
		{
			OnMousePositionUpdate();
		}

		if (CommonInput.GetMouseButtonDown(0))
		{
			OnMouseDown();
		}
	}

	public void OnSave()
	{

		if (PositionsToCopy.Count == 0) return;

		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore, // Ignore null values
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
			Formatting = Formatting.Indented
		};

		if (false)
		{
			settings.Formatting = Formatting.None;
		}

		var Matrix = MatrixManager.AtPoint(PositionsToCopy[0].BetterBounds.Min, CustomNetworkManager.IsServer);
		List<BetterBounds> LocalArea = new List<BetterBounds>();
		List<GameGizmoModel> Gizmos = new List<GameGizmoModel>();
		foreach (var Position in PositionsToCopy)
		{
			var Local = Position.BetterBounds.ConvertToLocal(Matrix);

			var Size = Local.Maximum - Local.Minimum;
			Gizmos.Add(new GameGizmoModel()
			{
				Pos = (Local.Minimum + (Size / 2f)).ToSerialiseString(),
				Size = Size.ToSerialiseString(),
			});
			Local.Maximum += new Vector3(-0.5f, -0.5f, 0);
			Local.Minimum -= new Vector3(-0.5f, -0.5f, 0);
			LocalArea.Add(Local);
		}

		var Data =  MapSaver.MapSaver.SaveMatrix(false, Matrix.MetaTileMap, true, LocalArea);
		Data.PreviewGizmos = Gizmos;

		var StringData =JsonConvert.SerializeObject(Data, settings);

		Clipboard = StringData;
		GUIUtility.systemCopyBuffer = StringData;


	}

	public void OnLoad()
	{

		MapSaver.MapSaver.MatrixData data = null;
		//For now, we assume the clipboard?
		try
		{
			data = JsonConvert.DeserializeObject<MapSaver.MapSaver.MatrixData>(GUIUtility.systemCopyBuffer);
		}
		catch (Exception e)
		{
			Loggy.LogWarning( GUIUtility.systemCopyBuffer + " " + e.ToString() );
		}


		if (data == null)
		{
			data = JsonConvert.DeserializeObject<MapSaver.MapSaver.MatrixData>(Clipboard);
		}

		//PreviewGizmos

		Vector3? Offset00 = null;

		foreach (var Gizmo in data.PreviewGizmos)
		{
			if (Offset00 == null)
			{
				Offset00 = Gizmo.Pos.ToVector3() - (Gizmo.Size.ToVector3() / 2f);
			}
			else
			{
				var Contender =  Gizmo.Pos.ToVector3() - (Gizmo.Size.ToVector3() / 2f);
				if (Offset00.Value.magnitude > Contender.magnitude)
				{
					Offset00 = Contender;
				}
			}
		}

		if (ActiveMouseGrabber == null)
		{
			ActiveMouseGrabber = Instantiate(MouseGrabberPrefab);
			ActiveMouseGrabber.OffsetRound = new Vector3(-0.5f, -0.5f, 0);
			ActiveMouseGrabber.SnapPosition = true;
		}



		foreach (var Gizmo in data.PreviewGizmos)
		{
			PreviewGizmos.Add(GameGizmomanager.AddNewSquareStaticClient(ActiveMouseGrabber.gameObject,
				(Gizmo.Pos.ToVector3() - Offset00.Value), Color.blue, BoxSize: Gizmo.Size.ToVector3()));
		}

		currentlyActivePaste = data;
	}

	[NaughtyAttributes.Button]
	public void OnMouseDown()
	{
		//Ignore spawn if pointer is hovering over GUI
		if (EventSystem.current.IsPointerOverGameObject()) return;

		if (currentlyActivePaste != null)
		{

			return;
		}

		if (StopSelectingButton.interactable)
		{
			if (ActiveGizmo == null)
			{
				var WorldPosition =GIZMO_OFFSET+ MouseUtils.MouseToWorldPos().RoundToInt();
				ActiveBoundStart = WorldPosition;
				ActiveBoundCurrent = WorldPosition;
				var Size = ActiveBoundCurrent - ActiveBoundStart;

				ActiveGizmo = GameGizmomanager.AddNewSquareStaticClient(null,
					ActiveBoundStart + (Size / 2f), Color.red, BoxSize: Size);
			}
			else
			{
				PositionsToCopy.Add( new GizmoAndBox()
				{
					BetterBounds = new BetterBounds(ActiveBoundStart, ActiveBoundCurrent),
					GameGizmoSquare = ActiveGizmo
				} );
				ActiveGizmo = null;
			}
		}
		else
		{
			var Pos = MouseUtils.MouseToWorldPos();
			var PositionsCopy = PositionsToCopy.ToList();
			PositionsCopy.Reverse();
			foreach (var Position in PositionsCopy)
			{
				if (Position.BetterBounds.Contains(Pos))
				{
					PositionsToCopy.Remove(Position);
					Position.GameGizmoSquare.Remove();
					break;
				}
			}
		}
	}

	public void OnMousePositionUpdate()
	{
		ActiveBoundCurrent =   ((MouseUtils.MouseToWorldPos()).RoundToInt() );

		var Size = ActiveBoundCurrent - ActiveBoundStart;

		if (Size.x > 0)
		{
			ActiveBoundCurrent.x += 0.5f;
		}
		else
		{
			ActiveBoundCurrent.x += -0.5f;
		}

		if (Size.y > 0)
		{
			ActiveBoundCurrent.y += 0.5f;
		}
		else
		{
			ActiveBoundCurrent.y += -0.5f;
		}

		Size = ActiveBoundCurrent - ActiveBoundStart;

		ActiveGizmo.Position = ActiveBoundStart + (Size / 2f);
		ActiveGizmo.Size = Size;

	}
	public void OnMouseButtonUp()
	{

	}

}

