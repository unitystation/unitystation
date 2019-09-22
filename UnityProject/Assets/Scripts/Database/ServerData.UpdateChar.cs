using System;
using System.Collections;
using UnityEngine;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static void UpdateCharacterProfile(CharacterSettings updateSettings, Action<string> callBack, Action<string> errorCallBack)
		{
			if (Instance.user == null)
			{
				Logger.LogWarning("User is not logged in! Skipping character upload.", Category.DatabaseAPI);
				return;
			}
			var json = JsonUtility.ToJson(updateSettings);
			var url = FirebaseRoot + $"/users/{Instance.user.UserId}/?updateMask.fieldPaths=character";
			Instance.StartCoroutine(Instance.TryUpdateChar(url, json, callBack, errorCallBack));
		}

		IEnumerator TryUpdateChar(string url, string jsonData, Action<string> callBack, Action<string> errorCallBack)
		{
			var payload = Newtonsoft.Json.JsonConvert.SerializeObject(new
			{
				fields = new
				{
					character = new { stringValue = jsonData }
				}
			});

			UnityWebRequest r = UnityWebRequest.Put(url, payload);
			r.method = "PATCH";
			r.SetRequestHeader("Content-Type", "application/json");
			r.SetRequestHeader("Authorization", $"Bearer {Instance.token}");

			yield return r.SendWebRequest();
			if (r.error != null)
			{
				Logger.Log("DB request failed: " + r.error, Category.DatabaseAPI);
				errorCallBack.Invoke(r.error);
			}
			else
			{
				callBack.Invoke(r.downloadHandler.text);
			}
		}
	}
}