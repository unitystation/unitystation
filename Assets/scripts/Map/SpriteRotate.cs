using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpriteRotate: MonoBehaviour {    
    private SpriteRenderer spriteRenderer;

    public Sprite[] sprites;

    private int index = 0;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Next() {
        if(sprites.Length > 0) {
            index = (index + 1) % sprites.Length;
            SetSprite(index);
        }
    }

    public void Previous() {
        if(sprites.Length > 0) {
            index = (index + sprites.Length - 1) % sprites.Length;
            SetSprite(index);
        }
    }

    private void SetSprite(int index) {
        spriteRenderer.sprite = sprites[index];
    }
}
