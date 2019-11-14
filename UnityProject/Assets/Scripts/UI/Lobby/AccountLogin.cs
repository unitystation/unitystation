using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DatabaseAPI;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class AccountLogin : MonoBehaviour
	{
		//Account login screen:
		public InputField userNameInput;
		public InputField passwordInput;

		void Start()
		{
			if (PlayerPrefs.HasKey("lastLogin"))
			{
				userNameInput.text = PlayerPrefs.GetString("lastLogin");
			}
		}

		public async void TryLogin(Action<string> successAction, Action<string> errorAction)
		{
			FirebaseUser user;
			try
			{
				user = await FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(userNameInput.text,
						passwordInput.text);
			}
			catch (FirebaseException e)
			{
				Logger.LogError($"Sign in error: {e.Message}", Category.DatabaseAPI);
				errorAction?.Invoke(e.Message);
				passwordInput.text = "";
				return;
			}

			await user.ReloadAsync();

			if (!user.IsEmailVerified)
			{
				errorAction?.Invoke(" Email Not Verified");
				passwordInput.text = "";
				return;
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
				return;
			}

			string content = await response.Content.ReadAsStringAsync();
			Debug.Log(content);
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
					return;
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
				Debug.Log("NEW CHAR: " + newChar);
				PlayerManager.CurrentCharacterSettings = JsonUtility.FromJson<CharacterSettings>(newChar);
			}

			successAction.Invoke("Login success");
			PlayerPrefs.SetString("lastLogin", userNameInput.text);
			PlayerPrefs.Save();
			passwordInput.text = "";
		}

		public bool ValidLogin()
		{
			//Missing username or password
			if (string.IsNullOrEmpty(userNameInput.text) || string.IsNullOrEmpty(passwordInput.text))
			{
				return false;
			}

			return true;
		}
	}
}