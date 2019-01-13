using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static void TryRetrieveCharSettings(Action<string> callBack, Action<string> errorCallBack)
		{
			Instance.StartCoroutine(Instance.TryRetrieveChar(callBack, errorCallBack));
		}

		IEnumerator TryRetrieveChar(Action<string> callBack, Action<string> errorCallBack)
		{
			UnityWebRequest r = UnityWebRequest.Get(URL_GetChar + PlayerPrefs.GetString("username"));
			r.SetRequestHeader("Cookie", sessionCookie);

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
					callBack.Invoke(apiResponse.message);
				}
			}
		}
	}
}
