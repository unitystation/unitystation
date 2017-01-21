using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewObject : MonoBehaviour {

    public Transform prefab;
    public Material PreviewMaterial;

    public Transform CreatePreview(Transform aPrefab) {
        Transform obj = (Transform) Instantiate(prefab);
        foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = PreviewMaterial;
        // If the building / object has some scripts or other components
        // which shouldn't be on the preview, remove them here:
        foreach(var script in obj.GetComponentsInChildren<MonoBehaviour>(true))
            Destroy(script);
        return obj;
    }
}
