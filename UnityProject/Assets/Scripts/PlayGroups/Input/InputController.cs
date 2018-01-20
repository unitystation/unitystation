using System.Collections.Generic;
using System.Linq;
using Cupboards;
using PlayGroup;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts.Tiles;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace PlayGroups.Input
{
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

		private Vector2 LastTouchedTile;
		private LayerMask layerMask;
		private ObjectBehaviour objectBehaviour;
		private PlayerMove playerMove;
		private PlayerSprites playerSprites;

		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(1, 0, 0, 0.5F);
			Gizmos.DrawCube(LastTouchedTile, new Vector3(1, 1, 1));
		}

		private void Start()
		{
			//for changing direction on click
			playerSprites = gameObject.GetComponent<PlayerSprites>();
			playerMove = GetComponent<PlayerMove>();
			objectBehaviour = GetComponent<ObjectBehaviour>();

			//Do not include the Default layer! Assign your object to one of the layers below:
			layerMask = LayerMask.GetMask("Furniture", "Walls", "Windows", "Machines", "Players", "Items", "Door Open", "Door Closed", "WallMounts",
				"HiddenWalls", "Objects");
		}

		private void Update()
		{
			//Needs to be rewritted to be server authoritative
			//Clients can currently ignore most intaract limitations (other than reach)
			CheckHandSwitch();
			CheckClick();
			CheckAltClick();
		}

		private void CheckHandSwitch()
		{
			if (UnityEngine.Input.GetMouseButtonDown(2))
			{
				UIManager.Hands.Swap();
			}
		}

		private void CheckClick()
		{
			if (UnityEngine.Input.GetMouseButtonDown(0) && !UnityEngine.Input.GetKey(KeyCode.LeftControl) && !UnityEngine.Input.GetKey(KeyCode.LeftAlt))
			{
				//change the facingDirection of player on click
				ChangeDirection();

				//if we found nothing at all to click on try to use whats in our hands (might be shooting at someone in space)
				if (!RayHit())
				{
					InteractHands();
				}
			}
		}

		private void CheckAltClick()
		{
			if (UnityEngine.Input.GetMouseButtonDown(0) && (UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt)))
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
			}
		}

		private void ChangeDirection()
		{
			Vector2 dir = (Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition) - transform.position).normalized;
			float angle = Angle(dir);
			if (!EventSystem.current.IsPointerOverGameObject() && playerMove.allowInput)
			{
				CheckPlayerDirection(angle);
			}
		}

		private bool RayHit()
		{
			Vector3 position = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);

			//for debug purpose, mark the most recently touched tile location
			LastTouchedTile = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));

			RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.zero, 10f, layerMask);

			//collect all the sprite renderers
			List<Renderer> renderers = new List<Renderer>();

			foreach (RaycastHit2D hit in hits)
			{
				Transform objectTransform = hit.collider.gameObject.transform;
				Renderer _renderer = IsHit(objectTransform, position - objectTransform.position);
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
					if (Interact(_renderer.transform, position))
					{
						isInteracting = true;
						break;
					}
				}
			}

			//Do interacts below: (This is because if a ray returns true but there is no interaction, check click
			//will not continue with the Interact call so we have to make sure it does below):
			if (!isInteracting) {
				//returning false then calls InteractHands from check click:
				return false;
			}

			//check if we found nothing at all
			return hits.Any();
		}

		private Renderer IsHit(Transform _transform, Vector3 hitPosition)
		{
			TilemapRenderer tilemapRenderer = _transform.GetComponent<TilemapRenderer>();

			if (tilemapRenderer)
			{
				return tilemapRenderer;
			}

			return IsPixelHit(_transform, hitPosition);
		}

		private SpriteRenderer IsPixelHit(Transform _transform, Vector3 hitPosition)
		{
			SpriteRenderer[] spriteRenderers = _transform.GetComponentsInChildren<SpriteRenderer>(false);

			//check order in layer for what should be triggered first
			//each item ontop of a table should have a higher order in layer
			SpriteRenderer[] bySortingOrder = spriteRenderers.OrderByDescending(sRenderer => sRenderer.sortingOrder).ToArray();

			foreach (SpriteRenderer spriteRenderer in bySortingOrder)
			{
				Sprite sprite = spriteRenderer.sprite;

				if (spriteRenderer.enabled && sprite)
				{
					Vector3 scale = spriteRenderer.gameObject.transform.localScale;
					Vector3 offset = spriteRenderer.gameObject.transform.localPosition;

					float pixelsPerUnit = sprite.pixelsPerUnit;

					float angle = spriteRenderer.gameObject.transform.parent.localEulerAngles.z * Mathf.Deg2Rad;

					float x = hitPosition.y * Mathf.Sin(angle) - hitPosition.x * Mathf.Cos(angle);
					float y = hitPosition.y * Mathf.Cos(angle) - hitPosition.x * Mathf.Sin(angle);

					int texPosX = Mathf.RoundToInt(sprite.rect.x + sprite.rect.width * 0.5f - (x / scale.x - offset.x % 1) * pixelsPerUnit);
					int texPosY = Mathf.RoundToInt(sprite.rect.y + (y / scale.y - offset.y % 1) * pixelsPerUnit + sprite.rect.height * 0.5f);

					Color pixelColor = sprite.texture.GetPixel(texPosX, texPosY);
					if (pixelColor.a > 0)
					{
						return spriteRenderer;
					}
				}
			}

			return null;
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
			if (PlayerManager.LocalPlayerScript.IsInReach(position))
			{
				//check the actual transform for an input trigger and if there is non, check the parent
				InputTrigger inputTrigger = _transform.GetComponentInParent<InputTrigger>();
				if (inputTrigger)
				{
					if (objectBehaviour.visibleState)
					{
						inputTrigger.Trigger(position);
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
			float angle = Angle(dir);
			CheckPlayerDirection(angle);
		}

		//Calculate the mouse click angle in relation to player(for facingDirection on PlayerSprites)
		private float Angle(Vector2 dir)
		{
			float angle = Vector2.Angle(Vector2.up, dir);

			if (dir.x < 0)
			{
				angle = 360 - angle;
			}

			return angle;
		}

		private void CheckPlayerDirection(float angle)
		{
			if (angle >= 315f && angle <= 360f || angle >= 0f && angle <= 45f)
			{
				playerSprites.CmdChangeDirection(Vector2.up);
				//Prediction
				playerSprites.FaceDirection(Vector2.up);
			}
			if (angle > 45f && angle <= 135f)
			{
				playerSprites.CmdChangeDirection(Vector2.right);
				//Prediction
				playerSprites.FaceDirection(Vector2.right);
			}
			if (angle > 135f && angle <= 225f)
			{
				playerSprites.CmdChangeDirection(Vector2.down);
				//Prediction
				playerSprites.FaceDirection(Vector2.down);
			}
			if (angle > 225f && angle < 315f)
			{
				playerSprites.CmdChangeDirection(Vector2.left);
				//Prediction
				playerSprites.FaceDirection(Vector2.left);
			}
		}
	}
}