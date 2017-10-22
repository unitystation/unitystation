using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PlayGroup;
using UI;


//Resos:
//0: 1024x640
//1: 1280x720
//2: 1920x1080 //FIXME: Mouse Screen to world problems for 1080p
public class DisplayManager : MonoBehaviour
{
	public static DisplayManager Instance;

    public Dropdown resoDropDown;
    public Light2D.LightingSystem lightingSystem;
    public Camera mainCamera;
	public FieldOfViewTiled fieldOfView;
	[Header("All canvas elements need to be added here")]
	public Canvas[] uiCanvases;
    private int width;
    private int height;

	private CanvasScaler canvasScaler;
	public Vector2 ScreenScale{
		get {
			if(canvasScaler == null){
				canvasScaler = GetComponentInParent<CanvasScaler>();
			}

			if(canvasScaler){
				return new Vector2(canvasScaler.referenceResolution.x / Screen.width, 
				                   canvasScaler.referenceResolution.y / Screen.height);
			} else {
				return Vector2.one;
			}
		}
	}
	void Awake(){
		if (Instance == null) {
			Instance = this;
		}
	}
    private void Start()
    {
   //     if (PlayerPrefs.HasKey("reso"))
   //     {
			//resoDropDown.value = PlayerPrefs.GetInt("reso");
			//SetResolution();
   //     }
   //     else
   //     {
			//resoDropDown.value = 1;
			//SetResolution();
        //}

		SetCameraFollowPos();
    }

	void OnEnable(){
		SceneManager.sceneLoaded += SetUpScene;
	}

	void OnDisable(){
		SceneManager.sceneLoaded -= SetUpScene;
	}

	void SetUpScene(Scene scene, LoadSceneMode mode)
	{
		if (GameData.IsInGame) {
			fieldOfView = FindObjectOfType<FieldOfViewTiled>();
		}
	}

    public void SetResolution()
    {
		int _value = resoDropDown.value;
        switch (_value){
			case 0:
				width = 1024;
				height = 640;
                break;
            case 1:
                width = 1280;
                height = 720;
                break;
            case 2:
                //FIXME: FOV edge and BG edge can be seen at this reso
                width = 1920;
                height = 1080;
                break;
        }
        PlayerPrefs.SetInt("reso", _value);
        Screen.SetResolution(width, height, false);
		if (GameData.IsInGame) {
			StartCoroutine(WaitForResoSet());
        }
    }

	IEnumerator WaitForResoSet(){
		yield return new WaitForSeconds(1f);
		SetCameraFollowPos();
	}

	public void SetCameraFollowPos(bool isPanelHidden = false){
		float xOffSet = Mathf.Abs(Camera.main.ScreenToWorldPoint(UIManager.Hands.transform.position).x - Camera2DFollow.followControl.transform.position.x)
			+ -0.06f;

		if (isPanelHidden) {
			xOffSet = -xOffSet;
		}
		Camera2DFollow.followControl.listenerObj.transform.localPosition = new Vector3(-xOffSet, 1f); //set listenerObj's position to player's pos
		Camera2DFollow.followControl.xOffset = xOffSet;
	}
}
