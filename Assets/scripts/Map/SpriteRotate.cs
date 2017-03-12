#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRotate: MonoBehaviour {

    public Sprite[] sprites = new Sprite[0];
    public Vector2[] positions = new Vector2[0];
    protected SpriteRenderer spriteRenderer;

    private int rotateIndex;
    public  int RotateIndex {
        get { return rotateIndex; }
        set {
            if(spriteRenderer && sprites.Length > 1) {
                rotateIndex = (value + sprites.Length) % sprites.Length;
                spriteRenderer.sprite = sprites[rotateIndex];
            }

            if(positions.Length > 1) {
                rotateIndex = (value + positions.Length) % positions.Length;
                transform.localPosition = positions[rotateIndex];
            }
        }
    }

    void Awake() {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    public void RotateForwards() {
        RotateIndex++;
    }

    public void RotateBackwards() {
        RotateIndex--;
    }
}
#endif
