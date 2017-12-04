using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockLightController : MonoBehaviour
{

    public Sprite spriteUnlocked;

    private Sprite spriteLocked;
    private SpriteRenderer spriteRenderer;

    private bool locked = true;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteLocked = spriteRenderer.sprite;
    }

    public void Lock()
    {
        locked = true;
        if (spriteRenderer != null)
            spriteRenderer.sprite = spriteLocked;
    }

    public void Unlock()
    {
        locked = false;
        if (spriteRenderer != null)
            spriteRenderer.sprite = spriteUnlocked;
    }

    public void Show()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    public void Hide()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    public bool IsLocked()
    {
        return locked;
    }
}
