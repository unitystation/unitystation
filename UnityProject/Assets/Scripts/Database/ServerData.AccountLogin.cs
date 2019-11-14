using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static void TryTokenValidation(string token, string uid, Action<string> successCallBack,
			Action<string> failedCallBack)
		{
			Instance.StartCoroutine(ValidateToken(token, uid, successCallBack, failedCallBack));
		}

		static IEnumerator ValidateToken(string token, string uid, Action<string> successCallBack,
			Action<string> failedCallBack)
		{
			var refreshToken = new RefreshToken();
			refreshToken.refreshToken = token;
			refreshToken.userID = uid;

			UnityWebRequest r = UnityWebRequest.Get("https://api.unitystation.org/validatetoken?data=" + JsonUtility.ToJson(refreshToken));
			Debug.Log(r.url);
			yield return r.SendWebRequest();


			if (r.error != null)
			{
				Logger.Log($"Encountered error while attempting to verify token: {r.error}", Category.DatabaseAPI);
				failedCallBack?.Invoke(r.error);
			}
			else
			{
				var response = JsonUtility.FromJson<ApiResponse>(r.downloadHandler.text);
				Auth.SignInWithCustomTokenAsync(response.message).ContinueWith(task =>
				{
					if (task.IsCanceled)
					{
						Logger.LogError("Custom token sign in was canceled.");
						failedCallBack?.Invoke("Custom token sign in was canceled.");
						return;
					}

					if (task.IsFaulted)
					{
						Logger.LogError("Task Faulted: " + task.Exception, Category.DatabaseAPI);
						failedCallBack?.Invoke("Error");
						return;
					}

					successCallBack?.Invoke("Success");
					Logger.Log("Signed in successfully with valid token", Category.DatabaseAPI);
				});
			}
		}
	}
}