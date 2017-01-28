using System;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRotate))]
public class PreviewObject: MonoBehaviour {
	private static PreviewObject instance;
    private GameObject prefab;
    public GameObject Prefab {
        get { return prefab; }
        set {
            if(prefab != value) {
                prefab = value;
                if(prefab)
                    spriteRotate.SetPrefab(prefab);

                if(SceneView.sceneViews.Count > 0) {
                    var sceneView = (SceneView) SceneView.sceneViews[0];
                    sceneView.Focus();
                }
            }
        }
    }

    private SpriteRotate spriteRotate;

    void Awake() {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(this);
		}
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