using UnityEngine;

public class LockLightController : MonoBehaviour
{
	private Sprite spriteLocked;
	public SpriteRenderer spriteRenderer;
	public Sprite spriteUnlocked;

	private void OnEnable()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteLocked = spriteRenderer.sprite;
	}

	public void Lock()
	{
		if (!CheckForSpriteRenderer()) return;

		spriteRenderer.sprite = spriteLocked;
	}

	public void Unlock()
	{
		if (!CheckForSpriteRenderer()) return;

		spriteRenderer.sprite = spriteUnlocked;
	}

	public void Show()
	{
		if (!CheckForSpriteRenderer()) return;

		spriteRenderer.enabled = true;
	}

	public void Hide()
	{
		if (!CheckForSpriteRenderer()) return;

		spriteRenderer.enabled = false;
	}

	bool CheckForSpriteRenderer()
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
		}

		if (spriteRenderer == null)
		{
			Logger.Log($"SpriteRender is not assigned for LockLightController on {gameObject.name}");
			return false;
		}

		return true;
	}
}