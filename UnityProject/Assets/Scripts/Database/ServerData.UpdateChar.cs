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
			var url = FirebaseRoot + "/users/doobly";
			Instance.StartCoroutine(Instance.TryUpdateChar(url, callBack, errorCallBack));
		}

		IEnumerator TryUpdateChar(string request, Action<string> callBack, Action<string> errorCallBack)
		{
			UnityWebRequest r = UnityWebRequest.Get(request);
			r.SetRequestHeader("Authorization", $"Bearer {Instance.token}");
	

			yield return r.SendWebRequest();
			if (r.error != null)
			{
				Logger.Log("DB request failed: " + r.error, Category.DatabaseAPI);
				errorCallBack.Invoke(r.error);
				Debug.Log(r.url);
			}
			else
			{
				Debug.Log(r.downloadHandler.text);
				callBack.Invoke(r.downloadHandler.text);

			}
		}
	}
}