using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

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

}