using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using System.Linq;
using UI;
using Weapons;

namespace InputControl {

	public class InputController: MonoBehaviour {
		private PlayerSprites playerSprites;
		private PlayerMove playerMove;

		private Vector2 LastTouchedTile;

		/// <summary>
		///  The minimum time limit between each action
		/// </summary>
		private float InputCooldownTimer = 0.01f;
		/// <summary>
		///  The cooldown before another action can be performed
		/// </summary>
		private float CurrentCooldownTime;

		void OnDrawGizmos() {
			if (LastTouchedTile != null) {
				Gizmos.color = new Color (1, 0, 0, 0.5F);
				Gizmos.DrawCube (LastTouchedTile, new Vector3 (1, 1, 1));
			}
		}

		void Start(){
			//for changing direction on click
			playerSprites = gameObject.GetComponent<PlayerSprites>();
			playerMove = GetComponent<PlayerMove>();
		}

		void Update() {
			if (CurrentCooldownTime > 0) {
				CurrentCooldownTime -= Time.deltaTime;
				//prevents the action taking longer than it should to occur
				if (CurrentCooldownTime < 0) {
					CurrentCooldownTime = 0;
				}
			}

			if (CurrentCooldownTime <= 0) {
				CurrentCooldownTime += InputCooldownTimer;
				CheckHandSwitch ();
				CheckClick ();
			}
		}

		private void CheckHandSwitch() {
			if (Input.GetMouseButtonDown(2)) {
				UIManager.Hands.Swap();
			}
		}

		private void CheckClick() {
			bool foundHit = false;

			if(Input.GetMouseButtonDown(0)) {
				foundHit = RayHit(Camera.main.ScreenToWorldPoint(Input.mousePosition));

				//change the facingDirection of player on click
				Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
				float angle = Angle(dir);
				if(!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && playerMove.allowInput)
					CheckPlayerDirection(angle);

				//if we found nothing at all to click on try to use whats in our hands (might be shooting at someone in space)
				if (!foundHit) {
					InteractHands();
				}
			}
		}

		private bool RayHit(Vector3 position) {
			//for debug purpose, mark the most recently touched tile location
			LastTouchedTile = new Vector2 (Mathf.Round(position.x), Mathf.Round(position.y));

			var hits = Physics2D.RaycastAll(position, Vector2.zero);

			//raycast all colliders and collect pixel hit gameobjects
			List<GameObject> hitGameObjects = new List<GameObject>();
			foreach (var hit in hits) {
				var objectTransform = hit.collider.gameObject.transform;
				var gameObjectHit = IsPixelHit(objectTransform, (position - objectTransform.position));
				if(gameObjectHit != null) {
					hitGameObjects.Add(gameObjectHit);
				}
			}

			//collect all the sprite renderers
			List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
			foreach (var hitGameObject in hitGameObjects) {
				var spriteRenderer = hitGameObject.GetComponent<SpriteRenderer>();
				if (spriteRenderer != null) {
					spriteRenderers.Add(spriteRenderer);
				}
			}

			//check which of the speite renderers we hit and pixel checked is the highest
			if (spriteRenderers.Count > 0) {
				var topSprite = spriteRenderers.OrderByDescending (sr => sr.sortingOrder).First ();
				if (topSprite != null) {
					Interact (topSprite.transform);
				}
			}

			//check if we found nothing at all
			if (hits != null && hits.Count() > 0) {
				return true;
			} else {
				return false;
			}
		}

		private GameObject IsPixelHit(Transform transform, Vector3 hitPosition) {
			var spriteRenderers = transform.GetComponentsInChildren<SpriteRenderer>(false);

			//check order in layer for what should be triggered first
			//each item ontop of a table should have a higher order in layer
			var bySortingOrder = spriteRenderers.OrderByDescending(sRenderer => sRenderer.sortingOrder).ToArray();

			foreach(var spriteRenderer in bySortingOrder) {
				var sprite = spriteRenderer.sprite;

				if(spriteRenderer.enabled && sprite) {
					var scale = spriteRenderer.gameObject.transform.localScale;
					var offset = spriteRenderer.gameObject.transform.localPosition;

					float pixelsPerUnit = sprite.pixelsPerUnit;

					int texPosX = Mathf.RoundToInt(sprite.rect.x + ((hitPosition.x / scale.x - offset.x % 1) * pixelsPerUnit + sprite.rect.width * 0.5f));
					int texPosY = Mathf.RoundToInt(sprite.rect.y + ((hitPosition.y / scale.y - offset.y % 1) * pixelsPerUnit + sprite.rect.height * 0.5f));


					var pixelColor = sprite.texture.GetPixel(texPosX, texPosY);
					if(pixelColor.a > 0) {
						return spriteRenderer.gameObject;
					}
				}
			}

			return null;
		}

		private void Interact(Transform transform) {
			//attempt to trigger the things in range we clicked on
			if (PlayerManager.LocalPlayerScript.IsInReach (transform)) {
				//check the actual transform for an input trigger and if there is non, check the parent
				var inputTrigger = transform.GetComponent<InputTrigger> ();
				if (inputTrigger) {
					inputTrigger.Trigger ();
					return;
				} else {
					inputTrigger = transform.parent.GetComponent<InputTrigger> ();
					if (inputTrigger) {
						inputTrigger.Trigger ();
						return;
					}
				}
			}

			//if we are holding onto an item like a gun attempt to shoot it if we were not in range to trigger anything
			InteractHands();
		}

		private void InteractHands() {
			if (UIManager.Hands.CurrentSlot.GameObject () != null) {
				var inputTrigger = UIManager.Hands.CurrentSlot.GameObject().GetComponent<InputTrigger> ();
				if (inputTrigger != null) {
					inputTrigger.Trigger ();
				}
			}
		}

		public void OnMouseDownDir(Vector2 dir){
			float angle = Angle(dir);
			CheckPlayerDirection(angle);
		}

		//Calculate the mouse click angle in relation to player(for facingDirection on PlayerSprites)
		float Angle(Vector2 dir)
		{
			if (dir.x < 0) {
				return 360 - (Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg * -1);
			} else {
				return Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
			}
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
