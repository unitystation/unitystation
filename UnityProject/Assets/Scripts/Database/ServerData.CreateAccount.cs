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
		///</summary>
		public static void CheckUsernameAvailable(string proposedName, Action<bool> callBack)
		{
			Instance.StartCoroutine(Instance.NameAvailability(proposedName, callBack));
		}

		IEnumerator NameAvailability(string proposedName, Action<bool> callBack)
		{
			UnityWebRequest request = UnityWebRequest.Get(URL_UsernameAvailability + proposedName);
			yield return request.SendWebRequest();
			if (request.error != null)
			{
				Logger.Log("DB request failed: " + request.error, Category.DatabaseAPI);
			}
			else
			{
				bool available = false;
				if (request.downloadHandler.text == "true")
				{
					available = true;
				}
				callBack.Invoke(available);
			}
		}
	}
}