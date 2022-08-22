using System;
using System.Collections;
using Shared.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;

/// <summary>
/// Controls loading screens (except for start up scene)
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
	private static LoadingScreenManager _loadingScreenManager;

	public static LoadingScreenManager Instance => FindUtils.LazyFindObject(ref _loadingScreenManager);

	[SerializeField] private LoadingScreen loadingScreen = null;


	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
		loadingScreen.gameObject.SetActive(false);
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
	}

	public void CloseLoadingScreen()
	{
		loadingScreen.gameObject.SetActive(false);
	}

	void OnSceneChange(Scene oldScene, Scene newScene)
	{
		CloseLoadingScreen();
	}

	public static void LoadFromLobby(Action endAction)
	{
		if (TileManager.TilesLoaded >= TileManager.TilesToLoad)
		{
			endAction.Invoke();
		}
		else
		{
			Instance.StartCoroutine(Instance.ShowTileManagerLoadingBar(endAction));
		}
	}

	IEnumerator ShowTileManagerLoadingBar(Action endAction)
	{
		loadingScreen.SetLoadBar(0f);
		loadingScreen.gameObject.SetActive(true);

		while (TileManager.TilesLoaded < TileManager.TilesToLoad)
		{
			loadingScreen.SetLoadBar((float)TileManager.TilesLoaded / (float)TileManager.TilesToLoad);
			yield return WaitFor.EndOfFrame;
		}

		endAction.Invoke();
	}
}
