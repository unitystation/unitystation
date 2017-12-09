using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayGroup
{
    public class HitIcon : MonoBehaviour
    {

        private Color transparent = new Color(1f, 1f, 1f, 0f);
        private Color visible = new Color(1f, 1f, 1f, 1f);
        private Vector3 lerpFrom;
        private Vector3 lerpTo;
        private SpriteRenderer spriteRenderer;

        private bool isFading = false;

        void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void ShowHitIcon(Vector2 dir, Sprite sprite)
        {
            if (isFading){
				return;
			}
                
            Vector3 newDir = new Vector3(dir.x, dir.y, 0f);
            lerpFrom = newDir * 0.75f;
            lerpTo = newDir;
            isFading = true;
			spriteRenderer.sprite = sprite;

            if (gameObject.activeInHierarchy) {
				StartCoroutine(FadeIcon());
			} 
        }

        IEnumerator FadeIcon()
        {
            float timer = 0f;
            float time = 0.1f;

            while (timer <= time)
            {
                timer += Time.deltaTime;
                float lerpProgress = timer / time;
                spriteRenderer.color = Color.Lerp(transparent, visible, lerpProgress);
                transform.localPosition = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress / 2f);
                yield return null;
            }
            lerpFrom = transform.localPosition;
            timer = 0f;
            time = 0.2f;

            while (timer <= time)
            {
                timer += Time.deltaTime;
                float lerpProgress = timer / time;
                spriteRenderer.color = Color.Lerp(visible, transparent, lerpProgress);
                transform.localPosition = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress);
                yield return null;
            }

            isFading = false;
        }
    }
}
