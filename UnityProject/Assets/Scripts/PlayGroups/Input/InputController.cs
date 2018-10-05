using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class InputController : MonoBehaviour
{
	/// <summary>
	///     The cooldown before another action can be performed
	/// </summary>
	private float CurrentCooldownTime;

	/// <summary>
	///     The minimum time limit between each action
	/// </summary>
	private float InputCooldownTimer = 0.01f;

	private Dictionary<Vector2, Color> LastTouchedTile = new Dictionary<Vector2, Color>();
	//private Vector2 LastTouchedTile;
	private LayerMask layerMask;
	private ObjectBehaviour objectBehaviour;
	private PlayerMove playerMove;
	private PlayerSprites playerSprites;

	public static readonly Vector3 sz = new Vector3(0.02f, 0.02f, 0.02f);
	private void OnDrawGizmos()
	{
		foreach (var info in LastTouchedTile)
		{
			Gizmos.color = info.Value;
			Gizmos.DrawCube(info.Key, sz);
		}
	}

	private void Start()
	{
		//for changing direction on click
		playerSprites = gameObject.GetComponent<PlayerSprites>();
		playerMove = GetComponent<PlayerMove>();
		objectBehaviour = GetComponent<ObjectBehaviour>();

		//Do not include the Default layer! Assign your object to one of the layers below:
		layerMask = LayerMask.GetMask("Furniture", "Walls", "Windows", "Machines", "Players", "Items", "Door Open", "Door Closed", "WallMounts",
			"HiddenWalls", "Objects", "Matrix");
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(2))
		{
			CheckHandSwitch();
		}
		if (Input.GetMouseButtonDown(0))
		{
			if (!CheckAltClick())
			{
				if (!CheckThrow())
				{
					CheckClick();
				}
			}
		}
	}

	private void CheckHandSwitch()
	{
		UIManager.Hands.Swap();
	}

	private void CheckClick()
	{
		if (!UnityEngine.Input.GetKey(KeyCode.LeftControl) && !UnityEngine.Input.GetKey(KeyCode.LeftAlt))
		{
			//change the facingDirection of player on click
			ChangeDirection();

			//if we found nothing at all to click on try to use whats in our hands (might be shooting at someone in space)
			if (!RayHit() && !EventSystem.current.IsPointerOverGameObject())
			{
				InteractHands();
			}
		}
	}

	private bool CheckAltClick()
	{
		if (UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt))
		{
			//Check for items on the clicked possition, and display them in the Item List Tab, if they're in reach
			Vector3 position = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
			position.z = 0f;
			if (PlayerManager.LocalPlayerScript.IsInReach(position))
			{
				List<GameObject> objects = UITileList.GetItemsAtPosition(position);
				LayerTile tile = UITileList.GetTileAtPosition(position);
				ControlTabs.ShowItemListTab(objects, tile, position);
			}

			UIManager.SetToolTip = $"clicked position: {Vector3Int.RoundToInt(position)}";
			return true;
		}
		return false;
	}
	private bool CheckThrow()
	{
		//Ignore throw if pointer is hovering over GUI
		if (EventSystem.current.IsPointerOverGameObject())
		{
			return false;
		}

		if (UIManager.IsThrow)
		{
			var currentSlot = UIManager.Hands.CurrentSlot;
			if (!currentSlot.CanPlaceItem())
			{
				return false;
			}
			//Check for items on the clicked possition, and display them in the Item List Tab, if they're in reach
			Vector3 position = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
			position.z = 0f;
			currentSlot.Clear();
			//				Logger.Log( $"Requesting throw from {currentSlot.eventName} to {position}" );
			PlayerManager.LocalPlayerScript.playerNetworkActions
				.CmdRequestThrow(currentSlot.eventName, position, (int) UIManager.DamageZone);

			//Disabling throw button
			UIManager.Action.Throw();
			return true;
		}
		return false;
	}

	private void ChangeDirection()
	{
		Vector2 dir = (Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition) - transform.position).normalized;
		if (!EventSystem.current.IsPointerOverGameObject() && playerMove.allowInput)
		{
			playerSprites.ChangePlayerDirection(Orientation.From(dir));
		}
	}

	private bool RayHit()
	{
		Vector3 position = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);

		//for debug purpose, mark the most recently touched tile location
		//	LastTouchedTile = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));

		RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.zero, 10f, layerMask);

		//collect all the sprite renderers
		List<Renderer> renderers = new List<Renderer>();

		foreach (RaycastHit2D hit in hits)
		{
			Transform objectTransform = hit.collider.gameObject.transform;
			Renderer _renderer = IsHit(objectTransform);
			if (_renderer != null)
			{
				renderers.Add(_renderer);
			}
		}
		bool isInteracting = false;
		//check which of the sprite renderers we hit and pixel checked is the highest
		if (renderers.Count > 0)
		{
			foreach (Renderer _renderer in renderers.OrderByDescending(sr => sr.sortingOrder))
			{
				// If the ray hits a FOVTile, we can continue down (don't count it as an interaction)
				// Matrix is the base Tilemap layer. It is used for matrix detection but gets in the way 
				// of player interaction
				if (!_renderer.sortingLayerName.Equals("FieldOfView"))
				{
					if (Interact(_renderer.transform, position))
					{
						isInteracting = true;
						break;
					}
				}
			}
		}

		//Do interacts below: (This is because if a ray returns true but there is no interaction, check click
		//will not continue with the Interact call so we have to make sure it does below):
		if (!isInteracting)
		{
			//returning false then calls InteractHands from check click:
			return false;
		}

		//check if we found nothing at all
		return hits.Any();
	}

	private Renderer IsHit(Transform _transform)
	{
		TilemapRenderer tilemapRenderer = _transform.GetComponent<TilemapRenderer>();

		if (tilemapRenderer)
		{
			return tilemapRenderer;
		}

		return IsPixelHit(_transform);
	}

	private SpriteRenderer IsPixelHit(Transform _transform)
	{
		SpriteRenderer[] spriteRenderers = _transform.GetComponentsInChildren<SpriteRenderer>(false);

		//check order in layer for what should be triggered first
		//each item ontop of a table should have a higher order in layer
		SpriteRenderer[] bySortingOrder = spriteRenderers.OrderByDescending(sRenderer => sRenderer.sortingOrder).ToArray();

		for (var i = 0; i < bySortingOrder.Length; i++)
		{
			SpriteRenderer spriteRenderer = bySortingOrder[i];
			Sprite sprite = spriteRenderer.sprite;

			if (spriteRenderer.enabled && sprite)
			{
				Color pixelColor = new Color();
				GetSpritePixelColorUnderMousePointer(spriteRenderer, out pixelColor);
				if (pixelColor.a > 0)
				{
					//debug the pixel get from mouse position:
					//if (_transform.gameObject.name.Contains("xtingu"))
					//{
					//	var mousePos = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
					//	if (!LastTouchedTile.ContainsKey(mousePos))
					//	{
					//		LastTouchedTile.Add(mousePos, pixelColor);
					//	}
					//	return null;
					//}
					return spriteRenderer;
				}
			}
		}

		return null;
	}

	//Thank you StackOverflow :-)
	public bool GetSpritePixelColorUnderMousePointer(SpriteRenderer spriteRenderer, out Color color)
	{
		color = new Color();
		Camera cam = Camera.main;
		Vector2 mousePos = Input.mousePosition;
		Vector2 viewportPos = cam.ScreenToViewportPoint(mousePos);
		if (viewportPos.x < 0.0f || viewportPos.x > 1.0f || viewportPos.y < 0.0f || viewportPos.y > 1.0f) return false; // out of viewport bounds
		// Cast a ray from viewport point into world
		Ray ray = cam.ViewportPointToRay(viewportPos);

		// Check for intersection with sprite and get the color
		return IntersectsSprite(spriteRenderer, ray, out color);
	}

	private bool IntersectsSprite(SpriteRenderer spriteRenderer, Ray ray, out Color color)
	{
		color = new Color();
		if (spriteRenderer == null) return false;
		Sprite sprite = spriteRenderer.sprite;
		if (sprite == null) return false;
		Texture2D texture = sprite.texture;
		if (texture == null) return false;
		// Check atlas packing mode
		if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
		{
			// Cannot use textureRect on tightly packed sprites
			Debug.LogError("SpritePackingMode.Tight atlas packing is not supported!");
			// TODO: support tightly packed sprites
			return false;
		}
		// Craete a plane so it has the same orientation as the sprite transform
		Plane plane = new Plane(transform.forward, transform.position);
		// Intersect the ray and the plane
		float rayIntersectDist; // the distance from the ray origin to the intersection point
		if (!plane.Raycast(ray, out rayIntersectDist)) return false; // no intersection
		// Convert world position to sprite position
		// worldToLocalMatrix.MultiplyPoint3x4 returns a value from based on the texture dimensions (+/- half texDimension / pixelsPerUnit) )
		// 0, 0 corresponds to the center of the TEXTURE ITSELF, not the center of the trimmed sprite textureRect
		Vector3 spritePos = spriteRenderer.worldToLocalMatrix.MultiplyPoint3x4(ray.origin + (ray.direction * rayIntersectDist));
		Rect textureRect = sprite.textureRect;
		float pixelsPerUnit = sprite.pixelsPerUnit;
		float halfRealTexWidth = sprite.rect.width * 0.5f;
		float halfRealTexHeight = sprite.rect.height * 0.5f;

		int texPosX = (int) (sprite.rect.x + (spritePos.x * pixelsPerUnit + halfRealTexWidth));
		int texPosY = (int) (sprite.rect.y + (spritePos.y * pixelsPerUnit + halfRealTexHeight));

		// Check if pixel is within texture
		if (texPosX < 0 || texPosX < textureRect.x || texPosX >= Mathf.FloorToInt(textureRect.xMax)) return false;
		if (texPosY < 0 || texPosY < textureRect.y || texPosY >= Mathf.FloorToInt(textureRect.yMax)) return false;
		// Get pixel color
		color = texture.GetPixel(texPosX, texPosY);
		return true;
	}

	public bool Interact(Transform _transform)
	{
		return Interact(_transform, _transform.position);
	}

	public bool Interact(Transform _transform, Vector3 position)
	{
		if (playerMove.isGhost)
		{
			return false;
		}

		//attempt to trigger the things in range we clicked on
		if (PlayerManager.LocalPlayerScript.IsInReach(Camera.main.ScreenToWorldPoint(Input.mousePosition)))
		{
			//Check for melee triggers first:
			MeleeTrigger meleeTrigger = _transform.GetComponentInParent<MeleeTrigger>();
			if (meleeTrigger != null)
			{
				if (meleeTrigger.MeleeInteract(gameObject, UIManager.Hands.CurrentSlot.eventName))
				{
					return true;
				}
			}

			//check the actual transform for an input trigger and if there is non, check the parent
			InputTrigger inputTrigger = _transform.GetComponentInParent<InputTrigger>();
			if (inputTrigger)
			{
				if (objectBehaviour.visibleState)
				{
					inputTrigger.Trigger(position);

					//FIXME currently input controller only uses the first InputTrigger found on an object
					/////// some objects have more then 1 input trigger, like players for example
					/////// below is a solution that should be removed when InputController is refactored
					/////// to support multiple InputTriggers on the target object
					if (inputTrigger.gameObject.layer == 8)
					{
						//This is a player. Attempt to use the player based inputTrigger
						P2PInteractions playerInteractions = inputTrigger.gameObject.GetComponent<P2PInteractions>();
						if (playerInteractions != null)
						{
							playerInteractions.Trigger(position);
						}
					}
					return true;
				}
				//Allow interact with cupboards we are inside of!
				ClosetControl cCtrl = inputTrigger.GetComponent<ClosetControl>();
				if (cCtrl && cCtrl.transform.position == PlayerManager.LocalPlayerScript.transform.position)
				{
					inputTrigger.Trigger(position);
					return true;
				}
				return false;
			}
		}
		//if we are holding onto an item like a gun attempt to shoot it if we were not in range to trigger anything
		return InteractHands();
	}

	private bool InteractHands()
	{
		if (UIManager.Hands.CurrentSlot.Item != null && objectBehaviour.visibleState)
		{
			InputTrigger inputTrigger = UIManager.Hands.CurrentSlot.Item.GetComponent<InputTrigger>();
			if (inputTrigger != null)
			{
				inputTrigger.Trigger();
				return true;
			}
		}

		return false;
	}

	public void OnMouseDownDir(Vector2 dir)
	{
		playerSprites.ChangePlayerDirection(Orientation.From(dir));
	}
}