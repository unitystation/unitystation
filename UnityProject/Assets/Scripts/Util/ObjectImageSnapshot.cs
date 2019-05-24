using System;
using UnityEngine;

/// <summary>
/// Very, very experimental composite gameobject renderer.
/// Meant to render a texture for desired object to be shown in UI
/// </summary>
public class ObjectImageSnapshot : MonoBehaviour {
	public Camera ObjectImageCamera
	{
		get
		{
			if ( !objectImageCamera )
			{
				objectImageCamera = SnapshotCamera.Instance.Camera;
			}

			return objectImageCamera;
		}
	}
    public Camera objectImageCamera;
    [HideInInspector]
    public int objectImageLayer;

    public int snapshotTextureWidth = 32;
    public int snapshotTextureHeight = 32;
    public Vector3 defaultPosition = new Vector3(0, 0, 1);
    public Vector3 defaultRotation = Vector3.one;
    public Vector3 defaultScale = new Vector3(1, 1, 1);

    void SetLayerRecursively(GameObject o, int layer)
    {
        foreach (Transform t in o.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }


    public Texture2D TakeObjectSnapshot(GameObject prefab)
    {
        return TakeObjectSnapshot(prefab, defaultPosition, Quaternion.Euler(defaultRotation), defaultScale);
    }


    public Texture2D TakeObjectSnapshot(GameObject prefab, Vector3 position)
    {
        return TakeObjectSnapshot(prefab, position, Quaternion.Euler(defaultRotation), defaultScale);
    }


    public Texture2D TakeObjectSnapshot(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        // validate properties
        if (ObjectImageCamera == null)
            throw new InvalidOperationException("Object Image Camera must be set");

        if (objectImageLayer < 0 || objectImageLayer > 31)
            throw new InvalidOperationException("Object Image Layer must specify a valid layer between 0 and 31");


        // clone the specified game object so we can change its properties at will, and
        // position the object accordingly
        GameObject tempObject = Instantiate(prefab, position, rotation);
        tempObject.transform.localScale = scale;

        // set the layer so the render to texture camera will see the object
        SetLayerRecursively(tempObject, objectImageLayer);


        // get a temporary render texture and render the camera
        RenderTexture.ReleaseTemporary(ObjectImageCamera.targetTexture);
        ObjectImageCamera.targetTexture = RenderTexture.GetTemporary(snapshotTextureWidth, snapshotTextureHeight, 24);
        ObjectImageCamera.Render();

        // activate the render texture and extract the image into a new texture
        RenderTexture saveActive = RenderTexture.active;
        RenderTexture.active = ObjectImageCamera.targetTexture;
        Texture2D texture = new Texture2D(ObjectImageCamera.targetTexture.width, ObjectImageCamera.targetTexture.height);
        texture.ReadPixels(new Rect(0, 0, ObjectImageCamera.targetTexture.width, ObjectImageCamera.targetTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = saveActive;

        // clean up after ourselves
        DestroyImmediate(tempObject);

        return texture;
    }
}