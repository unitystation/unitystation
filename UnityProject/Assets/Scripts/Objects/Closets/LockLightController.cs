using UnityEngine;

public class LockLightController : MonoBehaviour
{
	private SpriteHandler spriteHandler;

	private SpriteRenderer spriteRenderer;

	private enum LockState
	{
		Locked,
		Unlocked,
		Emagged
	}

	private void OnEnable()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteHandler = GetComponent<SpriteHandler>();
	}

	public void Lock()
	{
		if (!CheckForSpriteRenderer()) return;

		spriteHandler.ChangeSprite((int) LockState.Locked);
	}

	public void Unlock()
	{
		if (!CheckForSpriteRenderer()) return;

		spriteHandler.ChangeSprite((int) LockState.Unlocked);
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
			Logger.Log($"SpriteRender is not assigned for LockLightController on {gameObject.name}", Category.Sprites);
			return false;
		}

		return true;
	}
}
