using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Logs;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;
using Systems.Character;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		public static async Task<bool> ValidateUser(FirebaseUser user, Action<string> errorAction)
		{
			if (GameData.IsHeadlessServer) return false;

			await user.ReloadAsync();

			if (user.IsEmailVerified == false)
			{
				errorAction?.Invoke("Email Not Verified");
				return false;
			}

			var req = new HttpRequestMessage(HttpMethod.Get, ServerData.UserFirestoreURL);
			req.Headers.Add("Authorization", $"Bearer {ServerData.IdToken}");

			CancellationToken cancellationToken = new CancellationTokenSource(120000).Token;

			HttpResponseMessage response;
			try
			{
				response = await SafeHttpRequest.SendAsync(req, cancellationToken);
			}
			catch (Exception e)
			{
				Loggy.LogError($"Error Accessing Firestore: {e.Message}", Category.DatabaseAPI);
				errorAction?.Invoke($"Error accessing Firestore. Check your console (F5)");
				return false;
			}

			string content = await response.Content.ReadAsStringAsync();
			FireStoreResponse fr = JsonConvert.DeserializeObject<FireStoreResponse>(content);
			FireStoreCharacter fireStoreChar = fr.fields.character;

			CharacterSheet characterSettings;
			var settingsValid = false;

			if (fireStoreChar == null)
			{
				// Make a new character since there isn't one in the database
				characterSettings = CharacterSheet.GenerateRandomCharacter();
			}
			else
			{
				string unescapedJson = Regex.Unescape(fireStoreChar.stringValue);
				Loggy.Log(unescapedJson);
				try
				{
					characterSettings = JsonConvert.DeserializeObject<CharacterSheet>(unescapedJson);
				}
				catch
				{
					Loggy.LogWarning($"Couldn't deserialise saved character settings.");
					characterSettings = CharacterSheet.GenerateRandomCharacter();
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
					Loggy.LogError($"Error when updating character", Category.DatabaseAPI);
					errorAction?.Invoke("Error when updating character");
					return false;
				}
			}

			PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, user.Email);
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
				url + Uri.EscapeDataString(JsonConvert.SerializeObject(refreshToken)));

			CancellationToken cancellationToken = new CancellationTokenSource(120000).Token;

			HttpResponseMessage res;
			try
			{
				res = await SafeHttpRequest.SendAsync(r, cancellationToken);
			}
			catch (Exception e)
			{
				//fail silently for local offline testing
				if (!GameData.Instance.OfflineMode)
				{
					Loggy.Log($"Something went wrong with token validation {e.Message}", Category.DatabaseAPI);
				}

				return null;
			}

			string msg = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<ApiResponse>(msg);
		}
	}
}
