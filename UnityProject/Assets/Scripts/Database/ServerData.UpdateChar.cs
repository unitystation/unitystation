using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static void UpdateCharacterProfile(CharacterSettings updateSettings, Action<string> callBack, Action<string> errorCallBack)
		{
			var json = JsonUtility.ToJson(updateSettings);
			Instance.StartCoroutine(Instance.TryUpdateChar(json, callBack, errorCallBack));
		}

		IEnumerator TryUpdateChar(string request, Action<string> callBack, Action<string> errorCallBack)
		{
			UnityWebRequest r = UnityWebRequest.Get(URL_UpdateChar + WWW.EscapeURL(request));
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