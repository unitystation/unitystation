using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static void AttemptLogin(string username, string _password,
			Action<string> successCallBack, Action<string> failedCallBack)
		{
			var newRequest = new RequestLogin
			{
				username = username,
					password = _password,
					apiKey = ApiKey
			};

			var request = JsonUtility.ToJson(newRequest);

			Instance.StartCoroutine(Instance.PreformLogin(request, successCallBack, failedCallBack));
		}

		IEnumerator PreformLogin(string request,
			Action<string> successCallBack, Action<string> errorCallBack)
		{
			UnityWebRequest r = UnityWebRequest.Get(URL_TryLogin + WWW.EscapeURL(request));
			yield return r.SendWebRequest();
			if (r.error != null)
			{
				Logger.Log("Login request failed: " + r.error, Category.DatabaseAPI);
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
					successCallBack.Invoke(apiResponse.message);
					string s = r.GetResponseHeader("set-cookie");
					sessionCookie = s.Split(';')[0];
				}
			}
		}
	}
}