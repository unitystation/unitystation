using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InGameGizmos;
using Logs;
using MapSaver;
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

	public Toggle NonmappedItems;

	public List<GizmoAndBox> PositionsToCopy = new List<GizmoAndBox>();


	public List<GameGizmoSquare> PreviewGizmos = new List<GameGizmoSquare>();

	public Vector3 ActiveBoundStart;
	public Vector3 ActiveBoundCurrent;

	public GameGizmoSquare ActiveGizmo;

	public string Clipboard;

	public MouseGrabber MouseGrabberPrefab;

	public MouseGrabber ActiveMouseGrabber; //TODO Destroy?


	public MapSaver.MapSaver.MatrixData currentlyActivePaste;

	public Vector3? Offset00 = null;

	public bool UseLocal = false;

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
		this.gameObject.SetActive(false);
	}


	public void Close()
	{
		this.gameObject.SetActive(false);
	}

	private void OnDisable()
	{
		OnEscape();
		if (Updating)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			Updating = false;
		}

		currentlyActivePaste = null;
		Offset00 = null;

		foreach (var Gizmo in PreviewGizmos)
		{
			Gizmo.Remove();
		}
		PreviewGizmos.Clear();

		foreach (var Gizmo in PositionsToCopy)
		{
			Gizmo.GameGizmoSquare.Remove();
		}
		PositionsToCopy.Clear();
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

		if (UseLocal == false)
		{
			ClientRequestsSaveMessage.Send(Gizmos, LocalArea, Matrix, false, NonmappedItems.isOn);
		}
		else
		{
			var Data =  MapSaver.MapSaver.SaveMatrix(false, Matrix.MetaTileMap, true, LocalArea);
			Data.PreviewGizmos = Gizmos;
			var StringData = JsonConvert.SerializeObject(Data, settings);
			ReceiveData(StringData);
		}
	}

	public void ReceiveData(string StringData)
	{
		Clipboard = StringData;
		GUIUtility.systemCopyBuffer = StringData;

		foreach (var Gizmo in PositionsToCopy)
		{
			Gizmo.GameGizmoSquare.Remove();
		}
		PositionsToCopy.Clear();

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

		Offset00 = new Vector3(-0.5f, -0.5f, 0f) -Offset00;

		if (ActiveMouseGrabber == null)
		{
			ActiveMouseGrabber = Instantiate(MouseGrabberPrefab);
			ActiveMouseGrabber.SnapPosition = true;
		}

		foreach (var Gizmo in data.PreviewGizmos)
		{
			PreviewGizmos.Add(GameGizmomanager.AddNewSquareStaticClient(ActiveMouseGrabber.gameObject,
				 (Gizmo.Pos.ToVector3() + Offset00.Value ), Color.blue, BoxSize: Gizmo.Size.ToVector3()));
		}

		currentlyActivePaste = data;

		if (Updating == false)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			Updating = true;
		}
	}

	[NaughtyAttributes.Button]
	public void OnMouseDown()
	{
		//Ignore spawn if pointer is hovering over GUI
		if (EventSystem.current.IsPointerOverGameObject()) return;

		if (currentlyActivePaste != null)
		{


			var Offset = ActiveMouseGrabber.gameObject.transform.position.ToLocal();

			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore, // Ignore null values
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
				Formatting = Formatting.None
			};

			settings.Formatting = Formatting.None;

			ClientRequestLoadMap.Send(
				JsonConvert.SerializeObject(currentlyActivePaste,settings),
				MatrixManager.AtPoint(MouseUtils.MouseToWorldPos(), CustomNetworkManager.IsServer).Matrix,
				Offset00.Value,
				Offset
				);
			//MapLoader.LoadSection( MatrixManager.AtPoint(MouseUtils.MouseToWorldPos(), CustomNetworkManager.IsServer) ,Offset00.Value, Offset, currentlyActivePaste);

			if (KeyboardInputManager.IsAltActionKeyPressed() == false)
			{
				currentlyActivePaste = null;

				foreach (var Gizmo in PreviewGizmos)
				{
					Gizmo.Remove();
				}
				PreviewGizmos.Clear();
				Destroy(ActiveMouseGrabber.gameObject);
			}

			return;
		}

		if (StopSelectingButton.interactable)
		{
			if (ActiveGizmo == null)
			{
				var WorldPosition = MouseUtils.MouseToWorldPos().RoundToInt();
				ActiveBoundStart = WorldPosition;
				ActiveBoundCurrent = WorldPosition;
				var Size = ActiveBoundCurrent - ActiveBoundStart;

				ActiveGizmo = GameGizmomanager.AddNewSquareStaticClient(null,
					ActiveBoundStart + (Size / 2f), Color.red, BoxSize: Size);
			}
			else
			{

				var data = new BetterBounds(ActiveBoundStart, ActiveBoundCurrent);

				data = data.ExpandAllDirectionsBy(0.5f);

				PositionsToCopy.Add( new GizmoAndBox()
				{
					BetterBounds = data,
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

		var data = new BetterBounds(ActiveBoundStart, ActiveBoundCurrent);

		data = data.ExpandAllDirectionsBy(0.5f);

		var Size = data.size;

		ActiveGizmo.Position = data.min + (Size / 2f);
		ActiveGizmo.Size = Size;

	}
	public void OnMouseButtonUp()
	{

	}

}

