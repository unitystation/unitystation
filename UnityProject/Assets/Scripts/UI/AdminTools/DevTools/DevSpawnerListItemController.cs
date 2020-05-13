using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

[RequireComponent(typeof(EscapeKeyTarget))]
public class DevSpawnerListItemController : MonoBehaviour
{
	public Image image;
	bool isPaletted = false;
	public List<Color> palette;
	public Text titleText;
	public Text detailText;
	public GameObject drawingMessage;

	// holds which item is currently selected, shared between instances of this component.
	private static DevSpawnerListItemController selectedItem;

	//prefab to use for our cursor when painting
	public GameObject cursorPrefab;
	// prefab to spawn
	private GameObject prefab;

	// sprite under cursor for showing what will be spawned
	private GameObject cursorObject;

	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	private LightingSystem lightingSystem;

	void Awake()
	{
		// unity doesn't support property blocks on ui renderers, so this is a workaround
		image.material = Instantiate(image.material);
	}

	private void OnEnable()
	{
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}


	/// <summary>
	/// Initializes it to display the document
	/// </summary>
	/// <param name="resultDoc">document to display</param>
	public void Initialize(DevSpawnerDocument resultDoc)
	{
		prefab = resultDoc.Prefab;
		Sprite toUse = prefab.GetComponentInChildren<SpriteRenderer>()?.sprite;
		if (toUse != null)
		{
			image.sprite = toUse;
			CheckAndApplyPalette();
		}

		detailText.text = "Prefab";

		titleText.text = resultDoc.Prefab.name;
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
			SpriteRenderer curRend = cursorObject.GetComponent<SpriteRenderer>();
			curRend.sprite = image.sprite;

			if (isPaletted)
			{
				curRend.material = prefab.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
				MaterialPropertyBlock block = new MaterialPropertyBlock();
				curRend.GetPropertyBlock(block);
				List<Vector4> pal = palette.ConvertAll((Color c) => new Vector4(c.r, c.g, c.b, c.a));
				block.SetVectorArray("_ColorPalette", pal);
				block.SetInt("_IsPaletted", 1);
				curRend.SetPropertyBlock(block);
			}

			UIManager.IsMouseInteractionDisabled = true;
			escapeKeyTarget.enabled = true;
			selectedItem = this;
			drawingMessage.SetActive(true);
			lightingSystem.enabled = false;
		}
	}

	private void CheckAndApplyPalette()
	{
		isPaletted = false;
		//image.material.SetInt("_IsPaletted", 0);

		ClothingV2 prefabClothing = prefab.GetComponent<ClothingV2>();
		if (prefabClothing != null)
		{
			palette = prefabClothing.GetPaletteOrNull();
			if (palette != null)
			{
				isPaletted = true;
				image.material.SetInt("_IsPaletted", 1);
				image.material.SetColorArray("_ColorPalette", palette.ToArray());
				palette = new List<Color>(image.material.GetColorArray("_ColorPalette"));
			}
		}

		if (!isPaletted)
		{
			image.material.SetInt("_IsPaletted", 0);
		}


	}

	/// <summary>
	/// Tries to spawn at the specified position. Lets you spawn anywhere, even impassable places. Go hog wild!
	/// </summary>
	private void TrySpawn()
	{
		Vector3Int position = cursorObject.transform.position.RoundToInt();
		position.z = 0;

		if (CustomNetworkManager.IsServer)
		{
			Spawn.ServerPrefab(prefab, position);
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{PlayerManager.LocalPlayer.ExpensiveName()} spawned a {prefab.name} at {position}", ServerData.UserID);
		}
		else
		{
			DevSpawnMessage.Send(prefab, (Vector3) position, ServerData.UserID, PlayerList.Instance.AdminToken);
		}
	}
}
