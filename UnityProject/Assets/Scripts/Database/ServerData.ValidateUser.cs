using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Lobby;
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

			var newChar = "";
			if (fr.fields.character == null)
			{
				var newCharacter = new CharacterSettings();
				newCharacter.Name = StringManager.GetRandomMaleName();
				newCharacter.username = user.DisplayName;
				newChar = JsonUtility.ToJson(newCharacter);
				var updateSuccess = await ServerData.UpdateCharacterProfile(newChar);

				if (!updateSuccess)
				{
					Logger.LogError($"Error when updating character", Category.DatabaseAPI);
					errorAction?.Invoke("Error when updating character");
					return false;
				}
			}

			if (string.IsNullOrEmpty(newChar))
			{
				var characterSettings =
					JsonUtility.FromJson<CharacterSettings>(Regex.Unescape(fr.fields.character.stringValue));
				PlayerPrefs.SetString("currentcharacter", fr.fields.character.stringValue);
				PlayerManager.CurrentCharacterSettings = characterSettings;
			}
			else
			{
				PlayerManager.CurrentCharacterSettings = JsonUtility.FromJson<CharacterSettings>(newChar);
			}

			successAction?.Invoke("Login success");
			PlayerPrefs.SetString("lastLogin", user.Email);
			PlayerPrefs.Save();
			return true;
		}

		public static async Task<ApiResponse> ValidateToken(RefreshToken refreshToken, bool doNotGenerateAccessToken = false)
		{
			var url = "https://api.unitystation.org/validatetoken?data=";

			if (doNotGenerateAccessToken)
			{
				url = "https://api.unitystation.org/validateuser?data=";
			}
			HttpRequestMessage r = new HttpRequestMessage(HttpMethod.Get, url + UnityWebRequest.EscapeURL(JsonUtility.ToJson(refreshToken)));

			CancellationToken cancellationToken = new CancellationTokenSource(120000).Token;

			HttpResponseMessage res;
			try
			{
				res = await HttpClient.SendAsync(r, cancellationToken);
			}
			catch(Exception e)
			{
				//fail silently for local offline testing
				if (!GameData.Instance.OfflineMode)
				{
					Logger.Log($"Something went wrong with token validation {e.Message}");
				}

				return null;
			}

			string msg = await res.Content.ReadAsStringAsync();
			return JsonUtility.FromJson<ApiResponse>(msg);
		}
	}
}