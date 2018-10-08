using System;
using System.Collections;
using System.Collections.Generic;
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
			RequestCreateAccount newRequest = new RequestCreateAccount
			{
				username = proposedName,
					password = _password,
					email = emailAcc,
					apiKey = ApiKey
			};

			var request = JsonUtility.ToJson(newRequest);
			Instance.StartCoroutine(Instance.AttemptCreation(request, callBack, errorCallBack));
		}

		IEnumerator AttemptCreation(string request, Action<string> callBack, Action<string> errorCallBack)
		{
			UnityWebRequest r = UnityWebRequest.Get(URL_TryCreate + WWW.EscapeURL(request));
			yield return r.SendWebRequest();
			if (r.error != null)
			{
				Logger.Log("DB request failed: " + r.error, Category.DatabaseAPI);
				errorCallBack.Invoke(r.error);
			} else {
				var apiResponse = JsonUtility.FromJson<ApiResponse>(r.downloadHandler.text);
				if(apiResponse.errorCode != 0){
					errorCallBack.Invoke(apiResponse.errorMsg);
				} else {
					string s = r.GetResponseHeader("set-cookie");
					sessionCookie = s.Split(';')[0];
					callBack.Invoke(apiResponse.message);
				}
			}
		}
	}
}