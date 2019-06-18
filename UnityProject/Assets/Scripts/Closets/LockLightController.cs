using UnityEngine;

public class LockLightController : MonoBehaviour
{
	private Sprite spriteLocked;
	public SpriteRenderer spriteRenderer;
	public Sprite spriteUnlocked;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteLocked = spriteRenderer.sprite;
	}

	public void Lock()
	{
		spriteRenderer.sprite = spriteLocked;
	}

	public void Unlock()
	{
		spriteRenderer.sprite = spriteUnlocked;
	}

	public void Show()
	{
		spriteRenderer.enabled = true;
	}

	public void Hide()
	{
		spriteRenderer.enabled = false;
	}
}