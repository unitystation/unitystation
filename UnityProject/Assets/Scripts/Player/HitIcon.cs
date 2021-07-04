using System.Collections;
using UnityEngine;


public class HitIcon : MonoBehaviour
{
	private readonly Color transparent = new Color(1f, 1f, 1f, 0f);
	private readonly Color visible = new Color(1f, 1f, 1f, 1f);
	private Vector3 lerpFrom;
	private Vector3 lerpTo;
	private SpriteRenderer spriteRenderer;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	/// <summary>
	/// Show the hit icon animation
	/// </summary>
	/// <param name="dir">direction of the animation in world space</param>
	/// <param name="sprite">sprite to show</param>
	public void ShowHitIcon(Vector2 dir, SpriteRenderer sourceSpriteRenderer, PlayerScript playerScript)
	{
		lerpFrom = transform.localPosition + (Vector3)(dir * 0.75f);
		lerpTo = transform.localPosition + (Vector3)(dir);
		MaterialPropertyBlock pb = new MaterialPropertyBlock();
		sourceSpriteRenderer.GetPropertyBlock(pb);

		spriteRenderer.sprite = sourceSpriteRenderer.sprite;
		spriteRenderer.SetPropertyBlock(pb);

		StartCoroutine(FadeIcon());
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
		Despawn.ClientSingle(gameObject);
	}
}
