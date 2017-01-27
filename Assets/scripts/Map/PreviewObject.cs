using System;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRotate))]
public class PreviewObject: MonoBehaviour {
    private GameObject prefab;
    public GameObject Prefab {
        get { return prefab; }
        set {
            if(prefab != value) {
                prefab = value;
                if(prefab) 
                    spriteRotate.SetPrefab(prefab);
            }
        }
    }

    private SpriteRotate spriteRotate;

    void Awake() {
        spriteRotate = GetComponent<SpriteRotate>();
    }

    public GameObject CreateGameObject(Vector3 position) {
        var gameObject = (GameObject) PrefabUtility.InstantiatePrefab(Prefab);
        gameObject.transform.position = position;

        var spriteRotate = gameObject.GetComponentInChildren<SpriteRotate>();
        if(spriteRotate)
            spriteRotate.SpriteIndex = this.spriteRotate.SpriteIndex;

        return gameObject;
    }

    public void SetActive(bool active) {
        gameObject.SetActive(active);
    }

    public void RotateForwards() {
        spriteRotate.RotateForwards();
    }

    public void RotateBackwards() {
        spriteRotate.RotateBackwards();
    }
}