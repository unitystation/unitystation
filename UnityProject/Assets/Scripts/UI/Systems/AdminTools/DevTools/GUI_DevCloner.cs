using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Main logic for the UI for cloning objects
/// </summary>
public class GUI_DevCloner : MonoBehaviour
{

	private enum State
	{
		INACTIVE,
		SELECTING,
		DRAWING
	}

	[Tooltip("Text which shows the current status of the cloner.")]
	public Text statusText;
	[Tooltip("Prefab to use as our cursor when painting.")]
	public GameObject cursorPrefab;

	// objects selectable for cloning
	private LayerMask layerMask;

	//current state
	private State state;

	//object selected for cloning
	private GameObject toClone;
	//cursor object showing the item to be spawned
	private GameObject cursorObject;
	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	private LightingSystem lightingSystem;

	void Awake()
	{

		layerMask = LayerMask.GetMask("Furniture", "Machines", "Unshootable Machines", "Items",
			"Objects");
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
		ToState(State.SELECTING);
	}

	private void ToState(State newState)
	{
		if (newState == state)
		{
			return;
		}
		if (state == State.DRAWING)
		{
			//stop drawing
			Destroy(cursorObject);
		}

		if (newState == State.SELECTING)
		{
			statusText.text = "Click to select object to clone (ESC to Cancel)";
			UIManager.IsMouseInteractionDisabled = true;
			lightingSystem.enabled = false;

		}
		else if (newState == State.DRAWING)
		{
			statusText.text = "Click to spawn selected object (ESC to Cancel)";
			UIManager.IsMouseInteractionDisabled = true;
			//just chosen to be spawned on the map. Put our object under the mouse cursor
			cursorObject = Instantiate(cursorPrefab, transform.root);
			cursorObject.GetComponent<SpriteRenderer>().sprite = toClone.GetComponentInChildren<SpriteRenderer>().sprite;
			lightingSystem.enabled = false;
		}
		else if (newState == State.INACTIVE)
		{
			statusText.text = "Click to select object to clone (ESC to Cancel)";
			UIManager.IsMouseInteractionDisabled = false;
			lightingSystem.enabled = true;
			gameObject.SetActive(false);
		}

		state = newState;
	}

	public void OnEscape()
	{
		if (state == State.DRAWING)
		{
			ToState(State.SELECTING);
		}
		else if (state == State.SELECTING)
		{
			ToState(State.INACTIVE);
		}

	}

	private void OnDisable()
	{
		ToState(State.INACTIVE);
	}

	public void Open()
	{
		ToState(State.SELECTING);
	}

	private void Update()
	{
		if (state == State.SELECTING)
		{
			// ignore when we are over UI
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}

			//check which objects we are over, pick the top one to spawn
			if (CommonInput.GetMouseButtonDown(0))
			{
				//NOTE: Avoiding multiple enumeration by converting IEnumerables to lists.
				var hitGOs = MouseUtils.GetOrderedObjectsUnderMouse(layerMask,
					go => go.GetComponent<CustomNetTransform>() != null).ToList();
				//warn about objects which cannot be cloned
				var nonPooledHits = hitGOs
					.Where(go => Spawn.DeterminePrefab(go) == null).ToList();
				if (nonPooledHits.Any())
				{
					foreach (GameObject nonPooled in nonPooledHits)
					{
						Logger.LogWarningFormat("Object {0} does not have a PoolPrefabTracker component and its name" +
						                        " did not match one of our existing prefabs " +
						                        "therefore cannot be cloned (because we wouldn't know which prefab to instantiate). " +
						                        "Please attach this component to the object and specify the prefab" +
						                        " to allow it to be cloned.", Category.ItemSpawn, nonPooled.name);
					}
				}

				var pooledHits = hitGOs.Where(go => Spawn.DeterminePrefab(go) != null).ToList();
				if (pooledHits.Any())
				{
					toClone = pooledHits.First();
					ToState(State.DRAWING);
				}

			}

		}
		else if (state == State.DRAWING)
		{
			cursorObject.transform.position = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
			if (CommonInput.GetMouseButtonDown(0))
			{
				Vector3Int position = cursorObject.transform.position.RoundToInt();
				position.z = 0;
				if (MatrixManager.IsPassableAt(position, false))
				{
					if (CustomNetworkManager.IsServer)
					{
						Spawn.ServerClone(toClone, position);
					}
					else
					{
						DevCloneMessage.Send(toClone, (Vector3) position, ServerData.UserID, PlayerList.Instance.AdminToken);
					}
				}
			}
		}
	}
}
