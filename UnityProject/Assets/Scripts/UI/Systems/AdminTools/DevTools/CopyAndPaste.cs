using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Utils;
using InGameGizmos;
using Logs;
using MapSaver;
using Newtonsoft.Json;
using Shared.Managers;
using TileManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
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

	public List<GameGizmoSquare> NotGoingToBeSavedGizmos = new List<GameGizmoSquare>();

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

	private bool DelayCut = false;

	private List<MatrixInfo> PreviouslySelectedMatrixs = new List<MatrixInfo>();

	public void UnselectMatrix()
	{
		foreach (var Matrix in PreviouslySelectedMatrixs)
		{
			if (Matrix != null)
			{
				foreach (var layers in Matrix.MetaTileMap.Layers)
				{
					if (layers.Value == null)
					{
						Loggy.Error("[DevCameraControls/ToggleMatrixCheck] - Layer is null. Are we grabbing matrices before loading any?");
						continue;
					}
					var TM = layers.Value.GetComponent<Tilemap>();
					if (TM != null)
					{
						TM.color = Color.white;
					}
				}
			}
		}
		PreviouslySelectedMatrixs.Clear();
	}

	public void UpdateSelectedMatrix(int val)
	{
		UnselectMatrix();
		var IDs = (TMP_Dropdown.GetSelected().Select(x=> x as CustomOption));
		List<MatrixInfo> Matrixs = new List<MatrixInfo>();

		foreach (var ID in IDs)
		{
			MatrixInfo Matrix = null;
			if (ID.ID == null)
			{
				if (PositionsToCopy.Count > 0)
				{
					Matrix =  MatrixManager.AtPoint(PositionsToCopy[0].BetterBounds.Min, CustomNetworkManager.IsServer);
				}
			}
			else
			{
				Matrix = MatrixManager.Get(ID.ID.Value);
			}
			Matrixs.Add(Matrix);
		}

		PreviouslySelectedMatrixs = Matrixs;
		foreach (var Matrix in Matrixs)
		{
			if (Matrix != null)
			{
				var colour = Colour.Orange;
				foreach (var Layers in Matrix.MetaTileMap.Layers)
				{
					var TM = Layers.Value.GetComponent<Tilemap>();
					if (TM != null)
					{
						TM.color = colour;
					}
				}
			}
		}
	}

	private void OnEnable()
	{

		UpdateDropDown();
		UpdateSelected(0);
	}

	public override void Awake()
	{
		TMP_Dropdown.onValueChanged.AddListener(UpdateSelectedMatrix);
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		MatrixManager.Instance.OnActiveMatricesChange += UpdateDropDown;
		base.Awake();
	}

	public class CustomOption : TMP_Dropdown.OptionData
	{
		public int? ID;
	}

	public void UpdateSelected(int val)
	{
		var IDS = (TMP_Dropdown.GetSelected().Select(x => x as CustomOption));
		if (IDS.Count() > 1)
		{
			TMP_InputField.interactable = true;
		}
		else
		{
			foreach (var ID in IDS)
			{
				if (ID != null)
				{
					TMP_InputField.interactable = false;
					TMP_InputField.text = ID.text;
				}
				else
				{
					TMP_InputField.interactable = true;
					TMP_InputField.text = "";
				}
			}
		}



		ReGenNotGoingToBeSavedGizmos();
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
		NonmappedItems.onValueChanged.AddListener(OnNonmappedItemsChange);
	}

	public void OnNonmappedItemsChange(bool newval)
	{
		ReGenNotGoingToBeSavedGizmos();
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


		foreach (var Gizmo in NotGoingToBeSavedGizmos)
		{
			Gizmo.Remove();
		}
		NotGoingToBeSavedGizmos.Clear();
		UnselectMatrix();

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
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore, // Ignore null values
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
			Formatting = Formatting.Indented
		};

		var ObjectsVisible = DevCameraControls.Instance.GetObjectsMappingVisible();
		var Layers = DevCameraControls.Instance.ReturnVisibleLayers();

		var IDs = (TMP_Dropdown.GetSelected().Select(x => x  as CustomOption));

		if (PositionsToCopy.Count == 0)
		{
			List<MatrixInfo> MatrixsToSave = new List<MatrixInfo>();

			foreach(var ID in IDs)
			{
				MatrixsToSave.Add(MatrixManager.Get(ID.ID.Value));
			}

			if (UseLocal.isOn == false)
			{
				ClientRequestsSaveMessage.Send(new List<GameGizmoModel>(), new List<BetterBounds>(), MatrixsToSave, UesCompact.isOn, NonmappedItems.isOn, Layers, ObjectsVisible, CutSection.isOn, TMP_InputField.text);
			}
			else
			{
				var Data =  MapSaver.MapSaver.SaveMap(MatrixsToSave.Select(x => x.MetaTileMap).ToList(),
					UesCompact.isOn,TMP_InputField.text ,
					false,new List<BetterBounds>(), NonmappedItems.isOn,Layers, ObjectsVisible, CutSection.isOn, new List<GameGizmoModel>());
				var StringData = JsonConvert.SerializeObject(Data, settings);
				ReceiveData(StringData);
			}

			return;
		}
		else
		{
			if (IDs.Count() > 1)
			{
				Loggy.Error("TODO Support taking a section out of multiple Matrixes");
				return;
			}

			var ID = IDs.First().ID;

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

			Chat.AddExamineMsg( PlayerManager.LocalPlayerObject, $" Saving Portion of {Matrix.Name} " );

			if (UseLocal.isOn == false)
			{
				ClientRequestsSaveMessage.Send(Gizmos, LocalArea, new List<MatrixInfo>(){Matrix}, UesCompact.isOn, NonmappedItems.isOn, Layers, ObjectsVisible, CutSection.isOn);
			}
			else
			{
				var Data =  MapSaver.MapSaver.SaveMatrix(UesCompact.isOn, Matrix.MetaTileMap, true, LocalArea,NonmappedItems.isOn,Layers, ObjectsVisible, CutSection.isOn, Gizmos  );
				var StringData = JsonConvert.SerializeObject(Data, settings);
				ReceiveData(StringData);
			}
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

		foreach (var Gizmo in NotGoingToBeSavedGizmos)
		{
			Gizmo.Remove();
		}
		NotGoingToBeSavedGizmos.Clear();
	}

	public void OnLoad()
	{

		MapSaver.MapSaver.MatrixData data = null;
		MapSaver.MapSaver.MapData dataMap = null;
		//For now, we assume the clipboard?
		try
		{
			data = JsonConvert.DeserializeObject<MapSaver.MapSaver.MatrixData>(GUIUtility.systemCopyBuffer);
			Clipboard = GUIUtility.systemCopyBuffer;
		}
		catch (Exception e)
		{
			Loggy.Warning( GUIUtility.systemCopyBuffer + " " + e.ToString() );
		}

		try
		{
			if (data?.MatrixName == null)
			{
				dataMap = JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(GUIUtility.systemCopyBuffer);
				Clipboard = GUIUtility.systemCopyBuffer;
			}
		}
		catch (Exception e)
		{
			Loggy.Warning( GUIUtility.systemCopyBuffer + " " + e.ToString() );
		}


		if (data?.MatrixName == null && dataMap?.MapName == null)
		{
			try
			{
				data = JsonConvert.DeserializeObject<MapSaver.MapSaver.MatrixData>(Clipboard);
			}
			catch (Exception e)
			{
				Loggy.Warning(e.ToString());
			}

		}

		if (data?.MatrixName == null && dataMap?.MapName == null)
		{
			dataMap = JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(Clipboard);
		}


		if (data?.MatrixName != null)
		{
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
		else
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore, // Ignore null values
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
				Formatting = Formatting.None
			};

			settings.Formatting = Formatting.None;

			var ObjectsVisible = DevCameraControls.Instance.GetObjectsMappingVisible();
			var Layers = DevCameraControls.Instance.ReturnVisibleLayers();

			ClientRequestLoadMap.Send(
				JsonConvert.SerializeObject(dataMap,settings),
				null,
				Vector3.zero,
				Vector3.zero,
				Layers,
				ObjectsVisible,
				dataMap.MapName,
				ClientRequestLoadMap.LoadType.LoadMap
			);
		}

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
			var ID = (TMP_Dropdown.GetSelected().FirstOrDefault() as CustomOption).ID;
			var MatrixName = TMP_InputField.text;
			if (ID != null)
			{
				Matrix = MatrixManager.Get(ID.Value);
				MatrixName = Matrix.Name;
				Offset = ActiveMouseGrabber.gameObject.transform.position.ToLocal(Matrix);
			}
			else
			{
				Offset = ActiveMouseGrabber.gameObject.transform.position - new Vector3(1f, 1f,0);
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

			var name = TMP_InputField.text;
			if (string.IsNullOrEmpty(name))
			{
				name = currentlyActivePaste.MatrixName;
			}

			ClientRequestLoadMap.Send(
				JsonConvert.SerializeObject(currentlyActivePaste,settings),
				Matrix?.Matrix,
				Offset00.Value,
				Offset,
				Layers,
				ObjectsVisible,
				name,
				ClientRequestLoadMap.LoadType.LoadMatrix
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
				ReGenNotGoingToBeSavedGizmos();
			}
		}
		else if (DelayCut == false)
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

			ReGenNotGoingToBeSavedGizmos();
			DelayCut = true;
			StartCoroutine(DelayInput());
		}
	}
	private IEnumerator DelayInput()
	{
		yield return WaitFor.Seconds(0.15f);
		DelayCut = false;
	}


	public void ReGenNotGoingToBeSavedGizmos()
	{

		foreach (var Square in NotGoingToBeSavedGizmos)
		{
			Square.Remove();
		}
		NotGoingToBeSavedGizmos.Clear();

		UpdateSelectedMatrix(0);

		if (NonmappedItems.isOn) return; //Everything is going to be saved
		if (PositionsToCopy.Count == 0) return; //Nothing selected

		var IDs = (TMP_Dropdown.GetSelected().Select(x => x  as CustomOption) );
		foreach (var ID in IDs)
		{
			MatrixInfo Matrix = MatrixManager.AtPoint(PositionsToCopy[0].BetterBounds.Min, CustomNetworkManager.IsServer);
			if (ID != null)
			{
				Matrix = MatrixManager.Get(ID.ID.Value);
			}

			foreach (var EtherealThing in Matrix.MetaDataLayer.EtherealThings)
			{
				foreach (var Boxes in PositionsToCopy)
				{
					if (Boxes.BetterBounds.Contains(EtherealThing.transform.position))
					{
						var Attribute = EtherealThing.GetComponentCustom<Attributes>();
						if (Attribute != null)
						{
							if (Attribute.IsMapped == false)
							{
								NotGoingToBeSavedGizmos.Add( GameGizmomanager.AddNewSquareStaticClient(EtherealThing.gameObject,
									Vector3.zero, Color.yellow));
							}
						}
						break;
					}
				}
			}

			var Objects = Matrix.MetaTileMap.ObjectLayer.GetTileList(CustomNetworkManager.IsServer)
				.AllObjects;


			foreach (var Object in Objects)
			{
				foreach (var Boxes in PositionsToCopy)
				{
					if (Boxes.BetterBounds.Contains(Object.transform.position))
					{
						var Attribute = Object.GetComponentCustom<Attributes>();
						if (Attribute != null)
						{
							if (Attribute.IsMapped == false)
							{
								NotGoingToBeSavedGizmos.Add( GameGizmomanager.AddNewSquareStaticClient(Object.gameObject,
									Vector3.zero, Color.yellow));
							}
						}
						break;
					}
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

