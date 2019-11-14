using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
		public Toggle autoLoginToggle;

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
			ServerData.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ServerData.IdToken}");

			HttpResponseMessage response;
			try
			{
				response = await ServerData.HttpClient.SendAsync(req);
			}
			catch (HttpRequestException e)
			{
				Logger.LogError($"Error Accessing Firestore: {e.Message}");
				return;
			}

			string content = await response.Content.ReadAsStringAsync();
			Debug.Log(content);
			FireStoreResponse fr = JsonUtility.FromJson<FireStoreResponse>(content);

			Debug.Log($"FR: Name: {fr.name} Error: {fr.error.message} character: {fr.fields.character.stringValue}");


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