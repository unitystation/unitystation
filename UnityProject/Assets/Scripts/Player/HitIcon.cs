using System.Collections;
using UnityEngine;


public class HitIcon : MonoBehaviour
{
	private readonly Color transparent = new Color(1f, 1f, 1f, 0f);
	private readonly Color visible = new Color(1f, 1f, 1f, 1f);
	private bool isFading;
	private Vector3 lerpFrom;
	private Vector3 lerpTo;
	private SpriteRenderer spriteRenderer;

	private void Start()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	/// <summary>
	/// Show the hit icon animation
	/// </summary>
	/// <param name="dir">direction of the animation in world space</param>
	/// <param name="sprite">sprite to show</param>
	public void ShowHitIcon(Vector2 dir, SpriteRenderer sourceSpriteRenderer)
	{
		if (isFading)
		{
			return;
		}
	

		Vector3 lerpFromWorld = transform.position + (Vector3)(dir * 0.75f);
		Vector3 lerpToWorld = transform.position + (Vector3)(dir);
		Vector3 lerpFromLocal = transform.parent.InverseTransformPoint(lerpFromWorld);
		Vector3 lerpToLocal = transform.parent.InverseTransformPoint(lerpToWorld);
		MaterialPropertyBlock pb = new MaterialPropertyBlock();
		sourceSpriteRenderer.GetPropertyBlock(pb);

		lerpFrom = lerpFromLocal;
		lerpTo = lerpToLocal;
		isFading = true;
		spriteRenderer.sprite = sourceSpriteRenderer.sprite;
		spriteRenderer.SetPropertyBlock(pb);
		

		if (gameObject.activeInHierarchy)
		{
			StartCoroutine(FadeIcon());
		}
	}

	private IEnumerator FadeIcon()
	{
		float timer = 0f;
		float time = 0.1f;

		while (timer <= time)
		{
			timer += Time.deltaTime;
			float lerpProgress = timer / time;
			spriteRenderer.color = Color.Lerp(transparent, visible, lerpProgress);
			transform.localPosition = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress / 2f);
			yield return WaitFor.EndOfFrame;
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
			yield return WaitFor.EndOfFrame;
		}
		spriteRenderer.sprite = null;
		isFading = false;
		transform.localPosition = Vector3.zero;
	}
}
