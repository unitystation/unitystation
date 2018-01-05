using Light2D;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DisplayManager : MonoBehaviour
{
	public static DisplayManager Instance;

	private CanvasScaler canvasScaler;
	public FieldOfViewTiled fieldOfView;
	private int height;
	public LightingSystem lightingSystem;
	public Camera mainCamera;

	public Dropdown resoDropDown;
	[Header("All canvas elements need to be added here")] public Canvas[] uiCanvases;
	private int width;

	public Vector2 ScreenScale
	{
		get
		{
			if (canvasScaler == null)
			{
				canvasScaler = GetComponentInParent<CanvasScaler>();
			}

			if (canvasScaler)
			{
				return new Vector2(canvasScaler.referenceResolution.x / Screen.width, canvasScaler.referenceResolution.y / Screen.height);
			}
			return Vector2.one;
		}
	}

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

	private void OnEnable()
	{
		SceneManager.sceneLoaded += SetUpScene;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= SetUpScene;
	}

	private void SetUpScene(Scene scene, LoadSceneMode mode)
	{
		if (GameData.IsInGame)
		{
			fieldOfView = FindObjectOfType<FieldOfViewTiled>();
		}
	}

	public void SetCameraFollowPos(bool isPanelHidden = false)
	{
		if(Camera2DFollow.followControl == null){
			return;
		}

		float xOffSet =
			Mathf.Abs(Camera.main.ScreenToWorldPoint(UIManager.Hands.transform.position).x - Camera2DFollow.followControl.transform.position.x) + -0.06f;

		if (isPanelHidden)
		{
			xOffSet = -xOffSet;
		}
		Camera2DFollow.followControl.listenerObj.transform.localPosition = new Vector3(-xOffSet, 1f); //set listenerObj's position to player's pos
		Camera2DFollow.followControl.xOffset = xOffSet;
	}
}