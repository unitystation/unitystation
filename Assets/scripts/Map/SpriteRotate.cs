#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRotate: MonoBehaviour {
    public Sprite[] sprites = new Sprite[0];
    private SpriteRenderer spriteRenderer;
    
    private int spriteIndex;
    public int SpriteIndex {
        get { return spriteIndex; }
        set {
			if(spriteRenderer && sprites.Length > 1 && !Application.isPlaying) {
                spriteIndex = (value + sprites.Length) % sprites.Length;
                spriteRenderer.sprite = sprites[SpriteIndex];
            }
        }
    }

    void Awake() {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    public void RotateForwards() {
        SpriteIndex++;
    }

    public void RotateBackwards() {
        SpriteIndex--;
    }
}
#endif
