using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
            if(spriteRenderer && sprites.Length > 1) {
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

    public void SetPrefab(GameObject prefab) {
        var spriteRotate = prefab.GetComponentInChildren<SpriteRotate>();
                
        for(int i = transform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if(spriteRotate) {
            spriteRenderer.enabled = true;
            sprites = spriteRotate.sprites;
            SpriteIndex = 0;

        } else {
            spriteRenderer.enabled = false;
            foreach(Transform child in prefab.transform) {
                var c = Instantiate(child.gameObject);

                foreach(var script in c.GetComponentsInChildren<MonoBehaviour>()) {
                    script.enabled = false;
                }

                c.transform.parent = transform;
                c.transform.localPosition = c.transform.position;
            }

            foreach(var renderer in GetComponentsInChildren<SpriteRenderer>()) {
                renderer.sharedMaterial = spriteRenderer.sharedMaterial;
                renderer.sortingLayerName = "Preview";
            }
        }
    }
}
