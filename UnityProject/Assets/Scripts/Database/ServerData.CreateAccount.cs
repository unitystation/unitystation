using System;
using UnityEngine;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		///<summary>
		///Tries to create an account for the user in player accounts
		///</summary>
		public static void TryCreateAccount(string proposedName, string _password, string emailAcc,
			Action<Firebase.Auth.FirebaseUser, CharacterSettings> callBack, Action<string> errorCallBack)
		{
			Instance.auth.CreateUserWithEmailAndPasswordAsync(emailAcc, _password).ContinueWith(task =>
			{
				if (task.IsCanceled)
				{
					errorCallBack.Invoke("Cancelled");
					//Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
					return;
				}
				if (task.IsFaulted)
				{
					errorCallBack.Invoke(task.Exception.Message);
					//Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
					return;
				}

				// Firebase user has been created.
				Firebase.Auth.FirebaseUser newUser = task.Result;

				Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
				{
					DisplayName = proposedName, //May be used for OOC chat, so find way to detect imposters
						PhotoUrl = null //TODO: set up later
				};

				newUser.UpdateUserProfileAsync(profile);

				Debug.LogFormat("Firebase user created successfully: {0} ({1})",
					newUser.DisplayName, newUser.UserId);

				var newCharacter = new CharacterSettings();
				callBack.Invoke(newUser, newCharacter);
			});
		}
	}
}