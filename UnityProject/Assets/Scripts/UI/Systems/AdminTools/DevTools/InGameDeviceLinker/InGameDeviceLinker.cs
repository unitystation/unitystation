using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Messages.Client.DevSpawner;
using Shared.Managers;
using Shared.Systems.ObjectConnection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EscapeKeyTarget))]
public class InGameDeviceLinker : SingletonManager<InGameDeviceLinker>
{
	//TODO UI of Name of selected master


	public Button StopSelectingButton;

	//prefab to use for our cursor when Connecting
	public DeviceLinkerCursor cursorPrefab;

	// sprite under cursor for showing what will be spawned
	private DeviceLinkerCursor cursorObject;


	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	private bool cachedLightingState;
	//get all components on matrix?
	//trhen dictionary MultitoolConnectionType
	//highlight always selected
	//Refresh button if it changes

	public Dictionary<MultitoolConnectionType, Dictionary<IMultitoolMasterable, List<IMultitoolSlaveable>>>
		MastersData =
			new Dictionary<MultitoolConnectionType, Dictionary<IMultitoolMasterable, List<IMultitoolSlaveable>>>();


	public Dictionary<IMultitoolSlaveable, GameGizmoLine> LinkGizmo = new Dictionary<IMultitoolSlaveable,GameGizmoLine>();
	public GameGizmoSquare MasterOrigin;
	public GameGizmoLine CursorLine;

	public bool Updating = false;
	private void OnEnable()
	{
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
	}

	public override void Start()
	{
		base.Start();
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

	}


	//IMultitoolSlaveable
	[NaughtyAttributes.Button]
	public void Refresh()
	{
		MastersData.Clear();

		foreach (var Matrix in MatrixManager.Instance.ActiveMatrices)
		{
			var Masters = Matrix.Value.GameObject.GetComponentsInChildren<IMultitoolMasterable>();
			foreach (var aMaster in Masters)
			{
				if (MastersData.ContainsKey(aMaster.ConType) == false)
				{
					MastersData[aMaster.ConType] = new Dictionary<IMultitoolMasterable, List<IMultitoolSlaveable>>();
				}

				if (MastersData[aMaster.ConType].ContainsKey(aMaster) == false)
				{
					MastersData[aMaster.ConType][aMaster] = new List<IMultitoolSlaveable>();
				}
			}
		}

		foreach (var Matrix in MatrixManager.Instance.ActiveMatrices)
		{
			var Slaves = Matrix.Value.GameObject.GetComponentsInChildren<IMultitoolSlaveable>();
			foreach (var aSlave in Slaves)
			{
				//TODO Handle multi-master
				if (aSlave.Master != null)
				{
					MastersData[aSlave.Master.ConType][aSlave.Master].Add(aSlave);
				}
			}
		}
		//SetupGizmosFor(MultitoolConnectionType.APC);
	}

	public void SetupGizmosFor(IMultitoolMasterable ShowFor)
	{
		MasterOrigin = GameGizmomanager.AddNewSquareStatic(ShowFor.gameObject, Vector3.zero, Color.cyan);


		foreach (var Device in MastersData[ShowFor.ConType][ShowFor])
		{
			if (Device is IMultitoolSlaveableMultiMaster MultimasterDevice) //TODO?
			{
				LinkGizmo[Device] = GameGizmomanager.AddNewLineStatic(Device.gameObject, Vector3.zero, ShowFor.gameObject,
					Vector3.zero, Color.blue);
				// foreach (var Master in MultimasterDevice.Masters)
				// {
				//
				// }
			}
			else
			{
				if (Device.Master == null) continue; //TODO Gizmo for not connected
				LinkGizmo[Device] = GameGizmomanager.AddNewLineStatic(Device.gameObject, Vector3.zero, ShowFor.gameObject,
					Vector3.zero, Color.green);
			}
		}
	}

	private void UpdateMe()
	{
		cursorObject.transform.position = MouseUtils.MouseToWorldPos();
		if (CommonInput.GetMouseButtonDown(0))
		{
			//Ignore spawn if pointer is hovering over GUI
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}

			TryConnectSelect();
		}
	}

	public void CloseButton()
	{
		this.gameObject.SetActive(false);
	}

	public void OnEscape()
	{
		//stop drawing
		if (Updating)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			Updating = false;
		}


		Destroy(cursorObject);
		UIManager.IsMouseInteractionDisabled = false;
		escapeKeyTarget.enabled = false;
		if (Camera.main.GetComponent<LightingSystem>() != null)
		{
			Camera.main.GetComponent<LightingSystem>().enabled = cachedLightingState;
		}

		CleanupGizmos();
		StopSelectingButton.interactable = false;
	}

	public void CleanupGizmos()
	{
		foreach (var Gizmo in LinkGizmo)
		{
			Gizmo.Value.Remove();
		}
		LinkGizmo.Clear();

		if (MasterOrigin != null)
		{
			MasterOrigin.Remove();
		}

		if (CursorLine != null)
		{
			CursorLine.Remove();
		}
	}

	[NaughtyAttributes.Button]
	public void OnSelected()
	{
		StopSelectingButton.interactable = true;
		Refresh();

		//just chosen to be spawned on the map. Put our object under the mouse cursor
		cursorObject = Instantiate(cursorPrefab, transform.root);

		cursorObject.InGameDeviceLinker = this;
		UIManager.IsMouseInteractionDisabled = true;
		escapeKeyTarget.enabled = true;
		cachedLightingState = Camera.main.GetComponent<LightingSystem>().enabled;
		Camera.main.GetComponent<LightingSystem>().enabled = false;
		if (Updating == false)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			Updating = true;
		}
	}

	/// <summary>
	/// Tries to spawn at the specified position. Lets you spawn anywhere, even impassable places. Go hog wild!
	/// </summary>
	private void TryConnectSelect()
	{
		Vector3Int position = cursorObject.transform.position.RoundToInt();
		var hit = MouseUtils.GetOrderedObjectsUnderMouse()?.FirstOrDefault();
		if (hit == null) return;
		if (CustomNetworkManager.IsServer)
		{
			if (cursorObject.SelectedMaster != null) //TODO multiple masters on one object "and maybe displaying the name of the class in the UI"
			{
				if (hit.gameObject == cursorObject.SelectedMaster.gameObject)
				{
					bool Found = false;
					bool NewMaster = false;
					var Masters = hit.GetComponents<IMultitoolMasterable>();
					foreach (var Master in Masters)
					{
						if (Master == cursorObject.SelectedMaster)
						{
							Found = true;
							continue;
						}

						if (Found)
						{
							NewMaster = true;
							CleanupGizmos();
							cursorObject.SelectedMaster = hit.GetComponent<IMultitoolMasterable>();
							if (cursorObject.SelectedMaster != null)
							{
								SetupGizmosFor(cursorObject.SelectedMaster);
								CursorLine = GameGizmomanager.AddNewLineStatic(cursorObject.SelectedMaster.gameObject, Vector3.zero, cursorObject.gameObject,
									Vector3.zero,new Color(1f,0f,1f,1f));
							}
						}
					}

					if (NewMaster == false)
					{
						cursorObject.SelectedMaster = null;
						CleanupGizmos();
					}
				}
				else
				{
					var Slaves = hit.GetComponents<IMultitoolSlaveable>();
					foreach (var Slave in Slaves)
					{
						if (Slave != null)
						{
							if (Slave.ConType != cursorObject.SelectedMaster.ConType) continue;
							if (Slave.Master == cursorObject.SelectedMaster) //un select
							{
								MastersData[Slave.Master.ConType][Slave.Master].Remove(Slave);
								LinkGizmo[Slave].Remove();
								LinkGizmo.Remove(Slave);
								Slave.SetMasterEditor(null);

							}
							else if (Slave.Master != cursorObject.SelectedMaster || Slave.Master == null)
							{
								Slave.SetMasterEditor(cursorObject.SelectedMaster);
								LinkGizmo[Slave] = GameGizmomanager.AddNewLineStatic(Slave.gameObject, Vector3.zero, Slave.Master.gameObject,
									Vector3.zero, Color.green);
								MastersData[Slave.Master.ConType][Slave.Master].Add(Slave);
							}
						}
					}
				}
			}
			else
			{
				cursorObject.SelectedMaster = hit.GetComponent<IMultitoolMasterable>();
				if (cursorObject.SelectedMaster != null)
				{
					SetupGizmosFor(cursorObject.SelectedMaster);
					CursorLine = GameGizmomanager.AddNewLineStatic(cursorObject.SelectedMaster.gameObject, Vector3.zero, cursorObject.gameObject,
						Vector3.zero,new Color(1f,0f,1f,1f));
				}
			}


			//var player = PlayerManager.LocalPlayerObject.Player();
			//UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord( //TODO
			//	$"{player.Username} spawned a {prefab.name} at {position}", player.UserId);
		}
		else
		{
			//DevSpawnMessage.Send(prefab, (Vector3) position, GUI_DevSpawner.Instance.StackAmount);
		}
	}
}