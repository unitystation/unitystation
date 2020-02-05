using Light2D;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DisplayManager : MonoBehaviour
{
	public static DisplayManager Instance;

	private CanvasScaler canvasScaler;

	private int height;

	private int width;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
	}

	private void Start()
	{
		SetCameraFollowPos();
	}

	public void SetCameraFollowPos()
	{
		if(Camera2DFollow.followControl == null){
			return;
		}

        float xOffSet =
             (Camera2DFollow.followControl.transform.position.x - Camera.main.ScreenToWorldPoint(UIManager.Hands.transform.position).x) * 1.38f;

		Camera2DFollow.followControl.listenerObj.transform.localPosition = new Vector3(-xOffSet, 1f); //set listenerObj's position to player's pos
		Camera2DFollow.followControl.SetXOffset(xOffSet);
	}
}