using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InputControl {

    public class InputController: MonoBehaviour {

        void Update() {
            CheckClick();
        }

        private void CheckClick() {
            if(Input.GetMouseButtonDown(0)) {
                RayHit(Camera.main.ScreenToWorldPoint(Input.mousePosition));
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
    }
}

