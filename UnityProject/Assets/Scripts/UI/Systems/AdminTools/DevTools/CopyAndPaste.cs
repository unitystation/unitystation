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
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EscapeKeyTarget))]
public class CopyAndPaste  : SingletonManager<CopyAndPaste>
{
	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	public Button StopSelectingButton;
	public Button StopUnSelectingButton;


	public Button SelectingButton;
	public Button UnSelectingButton;


	public Button Load;
	public Button Save;

	public TMP_Dropdown TMP_Dropdown;

	public TMP_InputField TMP_InputField;

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

	public Toggle UseLocal;

	public Toggle UesCompact;

	public Toggle CutSection;

	private void OnEnable()
	{
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		MatrixManager.Instance.OnActiveMatricesChange += UpdateDropDown;
		UpdateDropDown();
		UpdateSelected(0);
	}

	public class CustomOption : TMP_Dropdown.OptionData
	{
		public int? ID;
	}

	public void UpdateSelected(int val)
	{
		var ID = (TMP_Dropdown.options[TMP_Dropdown.value] as CustomOption).ID;
		if (ID != null)
		{
			TMP_InputField.interactable = false;
			TMP_InputField.text = TMP_Dropdown.options[TMP_Dropdown.value].text;
		}
		else
		{
			TMP_InputField.interactable = true;
			TMP_InputField.text = "";
		}
	}

	public void UpdateDropDown()
	{
		var Options = TMP_Dropdown.options;
		Options.Clear();
		Options.Add(new CustomOption()
		{
			ID = null,
			text = "New Matrix",
		});
		foreach (var Entry in MatrixManager.Instance.ActiveMatrices)

			Options.Add(new CustomOption()
			{
				ID = Entry.Key,
				text = Entry.Value.Name
			});
		TMP_Dropdown.options = Options;
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
		TMP_Dropdown.onValueChanged.AddListener(UpdateSelected);
	}


	public void Close()
	{
		this.gameObject.SetActive(false);
	}

	private void OnDisable()
	{
		MatrixManager.Instance.OnActiveMatricesChange -= UpdateDropDown;
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

		Load.interactable = false;
		Save.interactable = false;

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

		Load.interactable = false;
		Save.interactable = false;

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
		ActiveGizmo?.Remove();
		ActiveGizmo = null;
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
		Load.interactable = true;
		Save.interactable = true;
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

		var ID = (TMP_Dropdown.options[TMP_Dropdown.value] as CustomOption).ID;
		MatrixInfo Matrix = MatrixManager.AtPoint(PositionsToCopy[0].BetterBounds.Min, CustomNetworkManager.IsServer);
		if (ID != null)
		{
			Matrix = MatrixManager.Get(ID.Value);
		}


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

		var ObjectsVisible = DevCameraControls.Instance.GetObjectsMappingVisible();
		var Layers = DevCameraControls.Instance.ReturnVisibleLayers();


		Chat.AddExamineMsg( PlayerManager.LocalPlayerObject, $" Saving Portion of {Matrix.Name} " );

		if (UseLocal.isOn == false)
		{
			ClientRequestsSaveMessage.Send(Gizmos, LocalArea, Matrix, UesCompact.isOn, NonmappedItems.isOn, Layers, ObjectsVisible, CutSection.isOn);
		}
		else
		{
			var Data =  MapSaver.MapSaver.SaveMatrix(UesCompact.isOn, Matrix.MetaTileMap, true, LocalArea,NonmappedItems.isOn,Layers, ObjectsVisible, CutSection.isOn, Gizmos  );
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
			Clipboard = GUIUtility.systemCopyBuffer;
		}
		catch (Exception e)
		{
			Loggy.LogWarning( GUIUtility.systemCopyBuffer + " " + e.ToString() );
		}


		if (data == null)
		{
			data = JsonConvert.DeserializeObject<MapSaver.MapSaver.MatrixData>(Clipboard);
		}

		Offset00 = data.Get00Victor();

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
		escapeKeyTarget.enabled = true;

		StopSelectingButton.interactable = false;
		StopUnSelectingButton.interactable = false;
		UnSelectingButton.interactable = false;
		SelectingButton.interactable = false;
		Load.interactable =false;
		Save.interactable = false;
	}

	[NaughtyAttributes.Button]
	public void OnMouseDown()
	{
		//Ignore spawn if pointer is hovering over GUI
		if (EventSystem.current.IsPointerOverGameObject()) return;

		if (currentlyActivePaste != null)
		{

			MatrixInfo Matrix = null;
			Vector3 Offset = ActiveMouseGrabber.gameObject.transform.position.ToLocal();
			var ID = (TMP_Dropdown.options[TMP_Dropdown.value] as CustomOption).ID;
			var MatrixName = TMP_InputField.text;
			if (ID != null)
			{
				Matrix = MatrixManager.Get(ID.Value);
				MatrixName = Matrix.Name;
				Offset = ActiveMouseGrabber.gameObject.transform.position.ToLocal(Matrix);
			}




			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore, // Ignore null values
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
				Formatting = Formatting.None
			};

			settings.Formatting = Formatting.None;

			var ObjectsVisible = DevCameraControls.Instance.GetObjectsMappingVisible();
			var Layers = DevCameraControls.Instance.ReturnVisibleLayers();

			Chat.AddExamineMsg( PlayerManager.LocalPlayerObject, $" Loading map Data onto {MatrixName} " );

			ClientRequestLoadMap.Send(
				JsonConvert.SerializeObject(currentlyActivePaste,settings),
				Matrix?.Matrix,
				Offset00.Value,
				Offset,
				Layers,
				ObjectsVisible,
				TMP_InputField.text
				);

			if (KeyboardInputManager.IsAltActionKeyPressed() == false)
			{
				currentlyActivePaste = null;

				foreach (var Gizmo in PreviewGizmos)
				{
					Gizmo.Remove();
				}
				PreviewGizmos.Clear();
				Destroy(ActiveMouseGrabber.gameObject);

				escapeKeyTarget.enabled = false;

				StopSelectingButton.interactable = false;
				StopUnSelectingButton.interactable = false;

				UnSelectingButton.interactable = true;
				SelectingButton.interactable = true;
				Load.interactable = true;
				Save.interactable = true;
				Offset00 = null;
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


				Load.interactable = false;
				Save.interactable = false;
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

