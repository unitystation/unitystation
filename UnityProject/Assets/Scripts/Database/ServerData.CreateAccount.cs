using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using UnityEngine;
using UnityEngine.Networking;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		///<summary>
		///Check to see if the username is available in player accounts
		///Send a method with bool parameter to the delegate which will be Invoked on the api response
		///Also send a method to the errorCallBack delegate incase of DB failure
		///</summary>
		public static void TryCreateAccount(string proposedName, string _password, string emailAcc,
			Action<string> callBack, Action<string> errorCallBack)
		{
			Instance.auth.CreateUserWithEmailAndPasswordAsync(emailAcc, _password).ContinueWith(task =>
			{
				if (task.IsCanceled)
				{
					Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
					return;
				}
				if (task.IsFaulted)
				{
					Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
					return;
				}

				// Firebase user has been created.
				Firebase.Auth.FirebaseUser newUser = task.Result;
				Debug.LogFormat("Firebase user created successfully: {0} ({1})",
					newUser.DisplayName, newUser.UserId);
			});
			
			//TODO: Am working to phase this out and move to firebase auth:
			/* 
			RequestCreateAccount newRequest = new RequestCreateAccount
			{
				username = proposedName,
					password = _password,
					email = emailAcc,
					apiKey = ApiKey
			};

			var request = JsonUtility.ToJson(newRequest);
			Instance.StartCoroutine(Instance.AttemptCreation(request, callBack, errorCallBack));
			*/
		}

		IEnumerator AttemptCreation(string request, Action<string> callBack, Action<string> errorCallBack)
		{
			UnityWebRequest r = UnityWebRequest.Get(URL_TryCreate + UnityWebRequest.EscapeURL(request));
			yield return r.SendWebRequest();
			if (r.error != null)
			{
				Logger.Log("DB request failed: " + r.error, Category.DatabaseAPI);
				errorCallBack.Invoke(r.error);
			}
			else
			{
				var apiResponse = JsonUtility.FromJson<ApiResponse>(r.downloadHandler.text);
				if (apiResponse.errorCode != 0)
				{
					errorCallBack.Invoke(apiResponse.errorMsg);
				}
				else
				{
					string s = r.GetResponseHeader("set-cookie");
					sessionCookie = s.Split(';') [0];
					callBack.Invoke(apiResponse.message);
				}
			}
		}

	}
}