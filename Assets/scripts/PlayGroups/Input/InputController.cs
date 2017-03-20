using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

namespace InputControl {

    public class InputController: MonoBehaviour {
		private PlayerSprites playerSprites;

		void Start(){
			//for changing direction on click
			playerSprites = gameObject.GetComponent<PlayerSprites>();
		}

        void Update() {
            CheckClick();
        }

        private void CheckClick() {
            if(Input.GetMouseButtonDown(0)) {
                RayHit(Camera.main.ScreenToWorldPoint(Input.mousePosition));
				Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
				float angle = Angle(dir);
				//change the facingDirection of player on click
				if(!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
				CheckPlayerDirection(angle);
            }
        }

        private void RayHit(Vector3 position) {
            var hit = Physics2D.Raycast(position, Vector2.zero);

            if(hit.collider != null) {
                var objectTransform = hit.collider.gameObject.transform;

                if(IsPixelHit(objectTransform, (position - objectTransform.position))) {
                    Interact(objectTransform);
                } else {
                    hit.collider.enabled = false;
                    RayHit(position);
                    hit.collider.enabled = true;
                }
            }  
        }

        private bool IsPixelHit(Transform transform, Vector3 hitPosition) {
            var spriteRenderers = transform.GetComponentsInChildren<SpriteRenderer>(false);
            
            foreach(var spriteRenderer in spriteRenderers) {
                var sprite = spriteRenderer.sprite;

                if(spriteRenderer.enabled && sprite) {
                    var scale = spriteRenderer.gameObject.transform.localScale;
                    var offset = spriteRenderer.gameObject.transform.localPosition;

                    float pixelsPerUnit = sprite.pixelsPerUnit;

                    int texPosX = Mathf.RoundToInt(sprite.rect.x + ((hitPosition.x / scale.x - offset.x % 1) * pixelsPerUnit + sprite.rect.width * 0.5f));
                    int texPosY = Mathf.RoundToInt(sprite.rect.y + ((hitPosition.y / scale.y - offset.y % 1) * pixelsPerUnit + sprite.rect.height * 0.5f));


                    var pixelColor = sprite.texture.GetPixel(texPosX, texPosY);
                    if(pixelColor.a > 0) {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Interact(Transform objectTransform) {
            var inputTrigger = objectTransform.GetComponent<InputTrigger>();
            if(inputTrigger) {
                inputTrigger.Trigger();
            }
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
				playerSprites.FaceDirection(Vector2.up);
			if (angle > 45f && angle <= 135f) 
				playerSprites.FaceDirection(Vector2.right);
			if (angle > 135f && angle <= 225f) 
				playerSprites.FaceDirection(Vector2.down);
			if (angle > 225f && angle < 315f) 
				playerSprites.FaceDirection(Vector2.left);
		}
    }
}

