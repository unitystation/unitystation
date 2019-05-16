using System;
using System.Collections;
using System.Linq;
using System.Text;
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
		protected IEnumerator DoActionWaitSceneLoad(Action action)
		{
			bool wait = true;
			SceneManager.sceneLoaded += StopWaiting;

			action();
			Logger.Log("Waiting for scene load", Category.Tests);

			yield return new WaitWhile(() => wait);
			SceneManager.sceneLoaded -= StopWaiting;
			yield return new WaitForFixedUpdate();

			void StopWaiting(Scene scene, LoadSceneMode mode) => wait = false;
		}

		protected IEnumerator DoActionWaitSceneLoad(IEnumerator action)
		{
			bool wait = true;
			SceneManager.sceneLoaded += StopWaiting;

			yield return action;
			Logger.Log("Waiting for scene load", Category.Tests);

			yield return new WaitWhile(() => wait);
			SceneManager.sceneLoaded -= StopWaiting;
			yield return new WaitForFixedUpdate();

			void StopWaiting(Scene scene, LoadSceneMode mode) => wait = false;
		}

		protected IEnumerator DoActionWaitSceneUnload(Action action)
		{
			bool wait = true;
			SceneManager.sceneUnloaded += StopWaiting;

			action();
			Logger.Log("Waiting for scene unload", Category.Tests);

			yield return new WaitWhile(() => wait);
			SceneManager.sceneUnloaded -= StopWaiting;
			yield return new WaitForFixedUpdate();

			void StopWaiting(Scene scene) => wait = false;
		}

		protected IEnumerator DoActionWaitSceneUnload(IEnumerator action)
		{
			bool wait = true;
			SceneManager.sceneUnloaded += StopWaiting;

			yield return action;
			Logger.Log("Waiting for scene unload", Category.Tests);

			yield return new WaitWhile(() => wait);
			SceneManager.sceneUnloaded -= StopWaiting;
			yield return new WaitForFixedUpdate();

			void StopWaiting(Scene scene) => wait = false;
		}

		protected IEnumerator ClickButton(params object[] objects)
		{
			return ClickButton(String.Concat(objects));
		}

		protected IEnumerator ClickButton(string buttonName, float passedRetrySeconds = 0)
		{
			Logger.Log($"Starting to search for button: {buttonName}", Category.Tests);
			float currentRetrySecs = passedRetrySeconds;
			while (currentRetrySecs <= RetrySeconds)
			{
				var obj = GameObject.Find(buttonName);
				if (obj != null)
				{
					yield return ClickButton(obj, currentRetrySecs);
					yield break;
				}
				yield return null;
				currentRetrySecs += Time.fixedDeltaTime;
			}
			throw new TimeoutException("Retry period exceeded");
		}

		protected IEnumerator ClickButton(GameObject gameObject, float passedRetrySeconds = 0)
		{
			Logger.Log("Starting to search for button component", Category.Tests);
			float currentRetrySecs = passedRetrySeconds;
			while (currentRetrySecs <= RetrySeconds)
			{
				var button = gameObject.GetComponent<Button>();
				if (button != null)
				{
					yield return ClickButton(button, currentRetrySecs);
					yield break;
				}
				yield return null;
				currentRetrySecs += Time.fixedDeltaTime;
			}
			throw new TimeoutException("Retry period exceeded");
		}

		protected IEnumerator ClickButton(Button button, float passedRetrySeconds = 0)
		{
			Logger.Log("Starting to click button", Category.Tests);
			float currentRetrySecs = passedRetrySeconds;
			while (currentRetrySecs <= RetrySeconds)
			{
				try
				{
					button.onClick.Invoke();
					Logger.Log("Clicked button", Category.Tests);
					yield break;
				}
				catch (NullReferenceException) { }
				yield return null;
				currentRetrySecs += Time.fixedDeltaTime;
			}
			throw new TimeoutException("Retry period exceeded");
		}

		protected void ListButtons()
		{
			var sb = new StringBuilder();
			sb.AppendLine("---- ---- ---- ----");
			sb.AppendLine("Buttons:");
			sb.AppendLine(
				String.Join(", \n\r",
				GameObject.FindObjectsOfType<Button>()
				.Where(b => b.IsInteractable())
				.Select(b => b.name)
				.OrderBy(n => n)));
			sb.AppendLine("---- ---- ---- ----");
			Logger.Log(sb.ToString(), Category.Tests);
		}
		#endregion
	}
}