using System;
using System.Collections;
using System.Linq;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tests
{
	abstract class PlayModeTest
	{
		protected float RetrySeconds = 3;

		protected abstract string Scene { get; }

		#region Scene Methods
		protected IEnumerator LoadSceneAndSetActive()
		{
			return LoadSceneAndSetActive(Scene);
		}

		protected IEnumerator LoadSceneAndSetActive(string sceneName)
		{
			yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
			SetActiveScene(sceneName);
		}

		protected void SetActiveScene(string sceneName)
		{
			var scene = SceneManager.GetSceneByName(sceneName);
			SceneManager.SetActiveScene(scene);
		}
		#endregion

		#region Button Methods
		protected IEnumerator ClickButtonWaitSceneLoad(string buttonName)
		{
			bool wait = true;
			SceneManager.sceneLoaded += StopWaiting;

			yield return ClickButton(buttonName);
			Debug.Log("Waiting for scene load");

			yield return new WaitWhile(() => wait);
			SceneManager.sceneLoaded -= StopWaiting;

			void StopWaiting(Scene scene, LoadSceneMode mode) => wait = false;
		}

		protected IEnumerator ClickButtonWaitSceneUnload(string buttonName)
		{
			bool wait = true;
			SceneManager.sceneUnloaded += StopWaiting;

			yield return ClickButton(buttonName);
			Debug.Log("Waiting for scene unload");

			yield return new WaitWhile(() => wait);
			SceneManager.sceneUnloaded -= StopWaiting;

			void StopWaiting(Scene scene) => wait = false;
		}

		protected IEnumerator ClickButton(params object[] objects)
		{
			return ClickButton(String.Concat(objects));
		}

		protected IEnumerator ClickButton(string buttonName, float passedRetrySeconds = 0)
		{
			Debug.Log($"Starting to search for button: {buttonName}");
			float currentRetrySecs = passedRetrySeconds;
			do
			{
				var obj = GameObject.Find(buttonName);
				if (obj != null)
				{
					yield return ClickButton(obj, currentRetrySecs);
					break;
				}
				yield return new WaitForFixedUpdate();
				currentRetrySecs += Time.fixedDeltaTime;
			} while (currentRetrySecs <= RetrySeconds);
		}

		protected IEnumerator ClickButton(GameObject gameObject, float passedRetrySeconds = 0)
		{
			Debug.Log("Starting to search for button component");
			float currentRetrySecs = passedRetrySeconds;
			do
			{
				var button = gameObject.GetComponent<Button>();
				if (button != null)
				{
					yield return ClickButton(button, currentRetrySecs);
					break;
				}
				yield return new WaitForFixedUpdate();
				currentRetrySecs += Time.fixedDeltaTime;
			} while (currentRetrySecs <= RetrySeconds);
		}

		protected IEnumerator ClickButton(Button button, float passedRetrySeconds = 0)
		{
			Debug.Log("Starting to click button");
			float currentRetrySecs = passedRetrySeconds;
			do
			{
				try
				{
					button.onClick.Invoke();
					Debug.Log("Clicked button");
					break;
				}
				catch (NullReferenceException) { }
				yield return new WaitForFixedUpdate();
				currentRetrySecs += Time.fixedDeltaTime;
			} while (currentRetrySecs <= RetrySeconds);
		}

		protected void ListButtons()
		{
			Debug.Log(
				"---- ---- ---- ----" + Environment.NewLine +
				"Buttons:");
			Debug.Log(String.Join(", \n\r",
				GameObject.FindObjectsOfType<Button>()
				.Where(b => b.IsInteractable())
				.Select(b => b.name)
				.OrderBy(n => n)));
		}
		#endregion
	}
}