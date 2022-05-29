using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static async Task<bool> ValidateUser(FirebaseUser user, Action<string> successAction,
			Action<string> errorAction)
		{
			if (GameData.IsHeadlessServer) return false;

			await user.ReloadAsync();

			if (!user.IsEmailVerified)
			{
				errorAction?.Invoke("Email Not Verified");
				return false;
			}

			HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, ServerData.UserFirestoreURL);
			req.Headers.Add("Authorization", $"Bearer {ServerData.IdToken}");

			CancellationToken cancellationToken = new CancellationTokenSource(120000).Token;

			HttpResponseMessage response;
			try
			{
				response = await ServerData.HttpClient.SendAsync(req, cancellationToken);
			}
			catch (Exception e)
			{
				Logger.LogError($"Error Accessing Firestore: {e.Message}", Category.DatabaseAPI);
				errorAction?.Invoke($"Error Accessing Firestore: {e.Message}");
				return false;
			}

			string content = await response.Content.ReadAsStringAsync();
			FireStoreResponse fr = JsonUtility.FromJson<FireStoreResponse>(content);
			FireStoreCharacter fireStoreChar = fr.fields.character;

			CharacterSettings characterSettings;
			var settingsValid = false;

			if (fireStoreChar == null)
			{
				// Make a new character since there isn't one in the database
				characterSettings = new CharacterSettings
				{
					Name = StringManager.GetRandomMaleName(),
					Username = user.DisplayName
				};
			}
			else
			{
				string unescapedJson = Regex.Unescape(fireStoreChar.stringValue);
				Logger.Log(unescapedJson);
				try
				{
					characterSettings = JsonConvert.DeserializeObject<CharacterSettings>(unescapedJson);
				}
				catch
				{
					Logger.LogWarning($"Couldn't deserialise saved character settings.");
					characterSettings = new CharacterSettings();
					characterSettings.Username = user.DisplayName;
					characterSettings.Name = StringManager.GetRandomMaleName();
				}

				// Validate and correct settings in case the customization options change
				settingsValid = true;
			}

			if (!settingsValid)
			{
				// Upload the corrected settings so they are valid next time
				bool updateSuccess = await UpdateCharacterProfile(characterSettings);
				if (!updateSuccess)
				{
					Logger.LogError($"Error when updating character", Category.DatabaseAPI);
					errorAction?.Invoke("Error when updating character");
					return false;
				}
			}

			// In case PlayerPrefs doesn't already have the settings
			string jsonChar = JsonConvert.SerializeObject(characterSettings);
			PlayerPrefs.SetString("currentcharacter", jsonChar);

			PlayerManager.CurrentCharacterSettings = characterSettings;

			successAction?.Invoke("Login success");
			PlayerPrefs.SetString("lastLogin", user.Email);
			PlayerPrefs.Save();
			return true;
		}

		public static async Task<ApiResponse> ValidateToken(RefreshToken refreshToken,
			bool doNotGenerateAccessToken = false)
		{
			var url = "https://api.unitystation.org/validatetoken?data=";

			if (doNotGenerateAccessToken)
			{
				url = "https://api.unitystation.org/validateuser?data=";
			}

			HttpRequestMessage r = new HttpRequestMessage(HttpMethod.Get,
				url + UnityWebRequest.EscapeURL(JsonUtility.ToJson(refreshToken)));

			CancellationToken cancellationToken = new CancellationTokenSource(120000).Token;

			HttpResponseMessage res;
			try
			{
				res = await HttpClient.SendAsync(r, cancellationToken);
			}
			catch (Exception e)
			{
				//fail silently for local offline testing
				if (!GameData.Instance.OfflineMode)
				{
					Logger.Log($"Something went wrong with token validation {e.Message}", Category.DatabaseAPI);
				}

				return null;
			}

			string msg = await res.Content.ReadAsStringAsync();
			return JsonUtility.FromJson<ApiResponse>(msg);
		}
	}
}
