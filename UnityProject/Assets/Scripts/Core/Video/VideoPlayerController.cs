using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoPlayerController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer = null;
    [SerializeField] private VideoClip nukeDetVid = null;
    [SerializeField] private VideoClip roundRestartVid = null;

    void Awake()
    {
		videoPlayer.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
	    SceneManager.activeSceneChanged += OnSceneChange;
    }

    private void OnDisable()
    {
	    SceneManager.activeSceneChanged -= OnSceneChange;
    }

    void OnSceneChange(Scene oldScene, Scene newScene)
    {
	    videoPlayer.clip = null;
	    videoPlayer.gameObject.SetActive(false);
    }

    public void PlayNukeDetVideo()
    {
	    if (GameData.IsHeadlessServer) return;

	    videoPlayer.clip = nukeDetVid;
	    videoPlayer.gameObject.SetActive(true);
	    videoPlayer.Play();
    }

    public void PlayRoundRestartVideo()
    {
	    if (GameData.IsHeadlessServer) return;

	    videoPlayer.clip = roundRestartVid;
	    videoPlayer.gameObject.SetActive(true);
	    videoPlayer.Play();
    }
}
