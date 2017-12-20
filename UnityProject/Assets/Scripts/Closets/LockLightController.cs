using UnityEngine;

public class LockLightController : MonoBehaviour
{
	private bool locked = true;

	private Sprite spriteLocked;
	private SpriteRenderer spriteRenderer;
	public Sprite spriteUnlocked;

	private void Start()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteLocked = spriteRenderer.sprite;
	}

	public void Lock()
	{
		locked = true;
		if (spriteRenderer != null)
		{
			spriteRenderer.sprite = spriteLocked;
		}
	}

	public void Unlock()
	{
		locked = false;
		if (spriteRenderer != null)
		{
			spriteRenderer.sprite = spriteUnlocked;
		}
	}

	public void Show()
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.enabled = true;
		}
	}

	public void Hide()
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.enabled = false;
		}
	}

	public bool IsLocked()
	{
		return locked;
	}
}