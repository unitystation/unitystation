using System;
using System.Collections;
using UnityEngine;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static void AttemptLogin(string username, string _password,
			Action<string> successCallBack, Action<string> failedCallBack)
		{
			var status = new Status();

			Instance.auth.SignInWithEmailAndPasswordAsync(username, _password).ContinueWith(task =>
			{
				if (task.IsFaulted)
				{
					failedCallBack.Invoke("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
					status.error = true;
					return;
				}
			});

			Instance.StartCoroutine(MonitorLogin(successCallBack, failedCallBack, status));
		}

		public string RefreshToks;

		[ContextMenu("Test Access Token")]
		void Test()
		{
			auth.SignInWithCustomTokenAsync(RefreshToks).ContinueWith(task =>
			{
				if (task.IsCanceled)
				{
					Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
					return;
				}

				if (task.IsFaulted)
				{
					Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
					return;
				}

				Firebase.Auth.FirebaseUser newUser = task.Result;
				Debug.LogFormat("User signed in successfully: {0} ({1})",
					newUser.DisplayName, newUser.UserId);
			});
		}

		static IEnumerator MonitorLogin(Action<string> successCallBack, Action<string> failedCallBack, Status status)
		{
			float timeOutTime = 8f;
			float timeOutCount = 0f;

			while (Auth.CurrentUser == null || string.IsNullOrEmpty(Instance.refreshToken))
			{
				timeOutCount += Time.deltaTime;
				if (timeOutCount >= timeOutTime || status.error)
				{
					if (!status.error)
					{
						Logger.Log("Log in timed out", Category.DatabaseAPI);
					}

					failedCallBack.Invoke("Check your username and password.");
					yield break;
				}

				yield return WaitFor.EndOfFrame;
			}

			var url = FirebaseRoot + $"/users/{Auth.CurrentUser.UserId}";
			UnityWebRequest r = UnityWebRequest.Get(url);
			r.SetRequestHeader("Authorization", $"Bearer {Instance.refreshToken}");

			yield return r.SendWebRequest();
			if (r.error != null)
			{
				Logger.Log("Failed to retrieve user character settings: " + r.error, Category.DatabaseAPI);
				failedCallBack.Invoke(r.error);
			}
			else
			{
				var charData = JsonUtility.FromJson<UserDocument>(r.downloadHandler.text);
				successCallBack.Invoke(charData.fields.character.stringValue);
			}
		}

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
						Logger.LogError("Custom token sign in encountered an error: " + task.Exception, Category.DatabaseAPI);
						failedCallBack?.Invoke("Error");
						return;
					}

					successCallBack?.Invoke("Success");
					Logger.Log("Signed in successfully with valid token", Category.DatabaseAPI);
				});
			}
		}
	}

	[Serializable]
	public class UserDocument
	{
		public string name;
		public UserFields fields;
	}

	[Serializable]
	public class CharacterField
	{
		public string stringValue;
	}

	[Serializable]
	public class UserFields
	{
		public CharacterField character;
	}

	[Serializable]
	public class AccessTokenResponse
	{
		public string access_token;
		public string id_token;
	}
}