using System.Collections.Generic;
using Items;
using Messages.Client.DevSpawner;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;


[RequireComponent(typeof(EscapeKeyTarget))]
public class DevSpawnerListItemController : MonoBehaviour
{
	public Image image;
	private bool isPaletted = false;
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
	private bool cachedLightingState;

	private void Awake()
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
			cursorObject.transform.position = MouseUtils.MouseToWorldPos();
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
			lightingSystem.enabled = cachedLightingState;
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

			curRend.material = prefab.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			curRend.GetPropertyBlock(block);
			if (isPaletted)
			{
				Debug.Assert(palette != null, "Palette must not be null on paletteable objects.");
				List<Vector4> pal = palette.ConvertAll((c) => new Vector4(c.r, c.g, c.b, c.a));
				block.SetVectorArray("_ColorPalette", pal);
				block.SetInt("_IsPaletted", 1);
				block.SetInt("_PaletteSize", pal.Count);
			}
			else
			{
				block.SetInt("_IsPaletted", 0);
			}
			curRend.SetPropertyBlock(block);

			UIManager.IsMouseInteractionDisabled = true;
			escapeKeyTarget.enabled = true;
			selectedItem = this;
			drawingMessage.SetActive(true);
			cachedLightingState = lightingSystem.enabled;
			lightingSystem.enabled = false;
		}
	}

	private void CheckAndApplyPalette()
	{
		isPaletted = false;
		//image.material.SetInt("_IsPaletted", 0);

		if (prefab.TryGetComponent(out ItemAttributesV2 prefabAttributes))
		{
			ItemsSprites sprites = prefabAttributes.ItemSprites;
			if (sprites.IsPaletted)
			{
				palette = sprites.Palette;
				Debug.Assert(palette != null, "Palette must not be null on paletteable objects.");

				isPaletted = true;
				image.material.SetInt("_IsPaletted", 1);
				image.material.SetInt("_PaletteSize", palette.Count);
				image.material.SetColorArray("_ColorPalette", palette.ToArray());
				palette = new List<Color>(image.material.GetColorArray("_ColorPalette"));
			}
			else
			{
				palette = null;
			}
		}

		if (isPaletted == false)
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

		if (CustomNetworkManager.IsServer)
		{
			Spawn.ServerPrefab(prefab, position);
			var player = PlayerManager.LocalPlayer.Player();
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
					$"{player.Username} spawned a {prefab.name} at {position}", player.UserId);
		}
		else
		{
			DevSpawnMessage.Send(prefab, (Vector3) position);
		}
	}
}
