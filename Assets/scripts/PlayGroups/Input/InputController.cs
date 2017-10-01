using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using System.Linq;
using UI;
using UnityEngine.EventSystems;
using Cupboards;

namespace InputControl
{
	public class InputController : MonoBehaviour
	{
		private PlayerSprites playerSprites;
		private PlayerMove playerMove;
		private LayerMask layerMask;
		private Vector2 LastTouchedTile;
		private ObjectBehaviour objectBehaviour;

		/// <summary>
		///  The minimum time limit between each action
		/// </summary>
		private float InputCooldownTimer = 0.01f;

		/// <summary>
		///  The cooldown before another action can be performed
		/// </summary>
		private float CurrentCooldownTime;

		void OnDrawGizmos()
		{
			Gizmos.color = new Color(1, 0, 0, 0.5F);
			Gizmos.DrawCube(LastTouchedTile, new Vector3(1, 1, 1));
		}

		void Start()
		{
			//for changing direction on click
			playerSprites = gameObject.GetComponent<PlayerSprites>();
			playerMove = GetComponent<PlayerMove>();
			objectBehaviour = GetComponent<ObjectBehaviour>();

			//Do not include the Default layer! Assign your object to one of the layers below:
			layerMask = LayerMask.GetMask("Furniture", "Walls", "Windows", "Machines",
				"Players", "Items", "Door Open", "Door Closed", "WallMounts", "HiddenWalls");
		}

		void Update()
		{
			CheckHandSwitch();
			CheckClick();
		}

		private void CheckHandSwitch()
		{
			if (Input.GetMouseButtonDown(2)) {
				UIManager.Hands.Swap();
			}
		}

		private void CheckClick()
		{
			if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl)) {
				//change the facingDirection of player on click
				changeDirection();

				//if we found nothing at all to click on try to use whats in our hands (might be shooting at someone in space)
				if (!RayHit()) {
					InteractHands();
				}
			}
		}

		private void changeDirection()
		{
			Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) -
						   transform.position).normalized;
			float angle = Angle(dir);
			if (!EventSystem.current.IsPointerOverGameObject() && playerMove.allowInput)
				CheckPlayerDirection(angle);
		}

		private bool RayHit()
		{
			var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			//for debug purpose, mark the most recently touched tile location
			LastTouchedTile = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));

			var hits = Physics2D.RaycastAll(position, Vector2.zero, 10f, layerMask);

			//collect all the sprite renderers
			List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

			foreach (var hit in hits) {
				var objectTransform = hit.collider.gameObject.transform;
				var gameObjectHit = IsPixelHit(objectTransform, (position - objectTransform.position));
				if (gameObjectHit != null) {
					var spriteRenderer = gameObjectHit.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null) {
						spriteRenderers.Add(spriteRenderer);
					}
				}
			}

			//check which of the sprite renderers we hit and pixel checked is the highest
			if (spriteRenderers.Count > 0) {
				foreach (var sprite in spriteRenderers.OrderByDescending(sr => sr.sortingOrder)) {
					if (sprite != null) {
						if (Interact(sprite.transform)) {
							break;
						}
					}
				}
			}

			//check if we found nothing at all
			return hits.Count() > 0;
		}

		private GameObject IsPixelHit(Transform _transform, Vector3 hitPosition)
		{
			var spriteRenderers = _transform.GetComponentsInChildren<SpriteRenderer>(false);

			//check order in layer for what should be triggered first
			//each item ontop of a table should have a higher order in layer
			var bySortingOrder = spriteRenderers.OrderByDescending(sRenderer => sRenderer.sortingOrder).ToArray();

			foreach (var spriteRenderer in bySortingOrder) {
				var sprite = spriteRenderer.sprite;

				if (spriteRenderer.enabled && sprite) {
					var scale = spriteRenderer.gameObject.transform.localScale;
					var offset = spriteRenderer.gameObject.transform.localPosition;

					float pixelsPerUnit = sprite.pixelsPerUnit;

					int texPosX = Mathf.RoundToInt(sprite.rect.x +
												   ((hitPosition.x / scale.x - offset.x % 1) * pixelsPerUnit +
													sprite.rect.width * 0.5f));
					int texPosY = Mathf.RoundToInt(sprite.rect.y +
												   ((hitPosition.y / scale.y - offset.y % 1) * pixelsPerUnit +
													sprite.rect.height * 0.5f));


					var pixelColor = sprite.texture.GetPixel(texPosX, texPosY);
					if (pixelColor.a > 0) {
						return spriteRenderer.gameObject;
					}
				}
			}

			return null;
		}

		private bool Interact(Transform _transform)
		{
			//attempt to trigger the things in range we clicked on
			if (PlayerManager.LocalPlayerScript.IsInReach(_transform)) {
				//check the actual transform for an input trigger and if there is non, check the parent
				var inputTrigger = _transform.GetComponent<InputTrigger>();
				if (inputTrigger) {
					if (objectBehaviour.visibleState) {
						inputTrigger.Trigger();
						return true;
					} else {
						return false;
					}
				} else {
					inputTrigger = _transform.parent.GetComponent<InputTrigger>();
					if (inputTrigger) {
						if (objectBehaviour.visibleState) {
							inputTrigger.Trigger();
							return true;
						} else {
							//Allow interact with all cupboards because you may be in one!
							ClosetControl cCtrl = inputTrigger.GetComponent<ClosetControl>();
							if (cCtrl) {
								inputTrigger.Trigger();
								return true;
							}
							return false;
						}
					}
				}
			}
			//if we are holding onto an item like a gun attempt to shoot it if we were not in range to trigger anything
			return InteractHands();
		}

		private bool InteractHands()
		{
			if (UIManager.Hands.CurrentSlot.GameObject() != null && objectBehaviour.visibleState) {
				var inputTrigger = UIManager.Hands.CurrentSlot.GameObject().GetComponent<InputTrigger>();
				if (inputTrigger != null) {
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
		float Angle(Vector2 dir)
		{
			var angle = Vector2.Angle(Vector2.up, dir);

			if (dir.x < 0) {
				angle = 360 - angle;
			}

			return angle;
		}

		void CheckPlayerDirection(float angle)
		{
			if (angle >= 315f && angle <= 360f || angle >= 0f && angle <= 45f)
				playerSprites.CmdChangeDirection(Vector2.up);
			if (angle > 45f && angle <= 135f)
				playerSprites.CmdChangeDirection(Vector2.right);
			if (angle > 135f && angle <= 225f)
				playerSprites.CmdChangeDirection(Vector2.down);
			if (angle > 225f && angle < 315f)
				playerSprites.CmdChangeDirection(Vector2.left);
		}
	}
}