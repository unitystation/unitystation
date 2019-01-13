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
			Action<string> successCallBack, Action<string> failedCallBack, bool autoLoginSetting)
		{
			var newRequest = new RequestLogin
			{
				username = username,
					password = _password,
					apiKey = ApiKey
			};

			Instance.StartCoroutine(Instance.PreformLogin(newRequest, successCallBack, failedCallBack, autoLoginSetting));
		}

		IEnumerator PreformLogin(RequestLogin request,
			Action<string> successCallBack, Action<string> errorCallBack, bool autoLoginSetting)
		{
			var requestData = JsonUtility.ToJson(request);
			UnityWebRequest r = UnityWebRequest.Get(URL_TryLogin + WWW.EscapeURL(requestData));
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
					GameData.IsLoggedIn = false;
					PlayerPrefs.SetString("username","");
					PlayerPrefs.SetString("cookie", "");
					PlayerPrefs.SetInt("autoLogin", 0);
					PlayerPrefs.Save();
					errorCallBack.Invoke(apiResponse.errorMsg);
				}
				else
				{
					string s = r.GetResponseHeader("set-cookie");
					sessionCookie = s.Split(';') [0];
					GameData.LoggedInUsername = request.username;
					GameData.IsLoggedIn = true;
					if (autoLoginSetting)
					{
						PlayerPrefs.SetString("username", request.username);
						PlayerPrefs.SetString("cookie", s);
						PlayerPrefs.SetInt("autoLogin", 1);
						PlayerPrefs.Save();
					}
					successCallBack.Invoke(apiResponse.message);
				}
			}
		}
	}
}