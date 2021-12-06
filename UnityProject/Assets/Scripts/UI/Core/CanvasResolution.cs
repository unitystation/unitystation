using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CanvasResolution : MonoBehaviour
{
    float aspectCache;
    void Awake()
    {
        aspectCache = Camera.main.aspect;
        AdjustReferenceResolution();
    }

    private void OnEnable()
    {
	    if (Application.isPlaying)
	    {
		    UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	    }
    }

    private void OnDisable()
    {
	    if (Application.isPlaying)
	    {
		    UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	    }
    }

    private void UpdateMe()
    {
        if (aspectCache != Camera.main.aspect)
        {
            AdjustReferenceResolution();
        }
    }

    void AdjustReferenceResolution()
    {
        aspectCache = Camera.main.aspect;
        Vector2 referenceResolution;

        if (Camera.main.aspect >= 1.7)// 16:9
            referenceResolution = new Vector2(1920f, 1080f);
        else if (Camera.main.aspect > 1.6)// 5:3
            referenceResolution = new Vector2(2560f, 1536f);
        else if (Camera.main.aspect == 1.6)// 16:10
            referenceResolution = new Vector2(2560f, 1600f);
        else if (Camera.main.aspect >= 1.5)// 3:2
            referenceResolution = new Vector2(1920f, 1280f);
        else// 4:3
            referenceResolution = new Vector2(2048, 1536f);

        GetComponent<CanvasScaler>().referenceResolution = referenceResolution;
    }
}