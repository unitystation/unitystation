using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Documents;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.UIElements;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

[RequireComponent(typeof(EscapeKeyTarget))]
public class DevSpawnerListItemController : MonoBehaviour
{
	public Image image;
	public Text titleText;
	public Text detailText;
	public GameObject drawingMessage;

	// holds which item is currently selected, shared between instances of this component.
	private static DevSpawnerListItemController selectedItem;

	//prefab to use for our cursor when painting
	public GameObject cursorPrefab;
	// prefab to spawn
	private GameObject prefab;
	private string hier;

	// sprite under cursor for showing what will be spawned
	private GameObject cursorObject;

	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	private LightingSystem lightingSystem;

	private void Start()
	{
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}


	/// <summary>
	/// Initializes it to display the document
	/// </summary>
	/// <param name="resultDoc">document to display</param>
	public void Initialize(Document resultDoc)
	{
		if (resultDoc.Get("type").Equals(DevSpawnerDocument.PREFAB_TYPE))
		{
			prefab = PoolManager.GetPrefabByName(resultDoc.Get("name"));
			Sprite toUse = prefab.GetComponentInChildren<SpriteRenderer>()?.sprite;
			if (toUse != null)
			{
				image.sprite = toUse;
			}

			detailText.text = "Prefab";
		}
		else
		{
			hier = resultDoc.Get("hier");
			Sprite toUse = UniItemUtils.GetInventoryIconSprite(hier);
			if (toUse != null)
			{
				image.sprite = toUse;
			}
			//determine which prefab this is for
			GameObject prefab = ClothFactory.GetClothPrefabForHier(hier);
			detailText.text = $"{prefab.name}\n{hier}";
		}
		titleText.text = resultDoc.Get("name");
	}

	private void Update()
	{
		if (selectedItem == this)
		{
			cursorObject.transform.position = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
			if (CommonInput.GetMouseButtonDown(0))
			{
				//Ignore spawn if pointer is hovering over GUI
				if (EventSystem.current.IsPointerOverGameObject())
				{
					return;
				}
				TrySpawn();
			}
		}
	}

	private void OnDisable()
	{
		OnEscape();
	}

	public void OnEscape()
	{
		if (selectedItem == this)
		{
			//stop drawing
			Destroy(cursorObject);
			UIManager.IsMouseInteractionDisabled = false;
			escapeKeyTarget.enabled = false;
			selectedItem = null;
			drawingMessage.SetActive(false);
			lightingSystem.enabled = true;
		}
	}



	public void OnSelected()
	{
		if (selectedItem != this)
		{
			if (selectedItem != null)
			{
				//tell the other selected one that it's time to stop
				selectedItem.OnEscape();
			}
			//just chosen to be spawned on the map. Put our object under the mouse cursor
			cursorObject = Instantiate(cursorPrefab, transform.root);
			cursorObject.GetComponent<SpriteRenderer>().sprite = image.sprite;
			UIManager.IsMouseInteractionDisabled = true;
			escapeKeyTarget.enabled = true;
			selectedItem = this;
			drawingMessage.SetActive(true);
			lightingSystem.enabled = false;
		}
	}

	/// <summary>
	/// Tries to spawn at the specified position, does not spawn if position is not valid (impassable)
	/// </summary>
	private void TrySpawn()
	{
		Vector3Int position = cursorObject.transform.position.RoundToInt();
		position.z = 0;
		if (MatrixManager.IsPassableAt(position, true))
		{
			if (CustomNetworkManager.IsServer)
			{
				if (hier != null)
				{
					ClothFactory.CreateCloth(hier, position);
				}
				else
				{
					PoolManager.PoolNetworkInstantiate(prefab, position);
				}

			}
			else
			{
				if (hier != null)
				{
					DevSpawnMessage.Send(hier, true, (Vector3) position);
				}
				else
				{
					DevSpawnMessage.Send(prefab.name, false, (Vector3) position);
				}

			}
		}
	}
}
