using System;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static async Task<bool> UpdateCharacterProfile(string updateSettings)
		{
			if (FirebaseAuth.DefaultInstance.CurrentUser == null)
			{
				Logger.LogWarning("User is not logged in! Skipping character upload.", Category.DatabaseAPI);
				return false;
			}

			var payload = Newtonsoft.Json.JsonConvert.SerializeObject(new
			{
				fields = new
				{
					character = new { stringValue = updateSettings }
				}
			});

			HttpRequestMessage r = new HttpRequestMessage(new HttpMethod("PATCH"),
				UserFirestoreURL + "/?updateMask.fieldPaths=character");
			var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
			r.Content = httpContent;

			HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {IdToken}");

			CancellationToken cancellationToken = new CancellationTokenSource(120).Token;

			HttpResponseMessage res;
			try
			{
				res = await HttpClient.SendAsync(r, cancellationToken);
			}
			catch (Exception e)
			{
				Logger.LogError($"Error occured when uploading character: {e.Message}", Category.DatabaseAPI);
				return false;
			}

			PlayerPrefs.SetString("currentcharacter", JsonUtility.ToJson(updateSettings));
			PlayerPrefs.Save();

			return true;
		}
	}
}