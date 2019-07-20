using System;
using System.Collections;
using UnityEngine;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		///<summary>
		///Tries to create an account for the user in player accounts
		///</summary>
		public static void TryCreateAccount(string proposedName, string _password, string emailAcc,
			Action<CharacterSettings> callBack, Action<string> errorCallBack)
		{
			var newCharacter = new CharacterSettings();
			newCharacter.Name = StringManager.GetRandomMaleName();
			
			Instance.isFirstTime = true;
			var status = new Status();

			Instance.auth.CreateUserWithEmailAndPasswordAsync(emailAcc, _password).ContinueWith(task =>
			{
				if (task.IsCanceled)
				{
					errorCallBack.Invoke("Cancelled");
					Instance.isFirstTime = false;
					status.error = true;
					return;
				}
				if (task.IsFaulted)
				{
					errorCallBack.Invoke(task.Exception.Message);
					Instance.isFirstTime = false;
					status.error = true;
					return;
				}
			});

			Instance.StartCoroutine(WaitForResponse(newCharacter, callBack, errorCallBack,
				status, proposedName));
		}

		static IEnumerator WaitForResponse(CharacterSettings character, Action<CharacterSettings> callBack,
			Action<string> errorCallBack, Status status, string proposedName)
		{
			//Timeout
			float timeOutTime = 60f;
			float timeCount = 0f;
			bool isAuthed = false;

			while (!isAuthed)
			{
				timeCount += Time.deltaTime;

				if (Instance.auth.CurrentUser != null)
				{
					if (!status.profileSet)
					{
						UpdateProfile(proposedName, status, callBack, errorCallBack);
						status.profileSet = true;
					}
					if (!string.IsNullOrEmpty(Instance.auth.CurrentUser.DisplayName))
					{
						isAuthed = true;
					}
				}

				if (timeCount >= timeOutTime || status.error)
				{
					break;
				}
				yield return WaitFor.EndOfFrame;
			}

			if (isAuthed)
			{
				character.username = proposedName;
				callBack.Invoke(character);
			}
			else
			{
				Instance.isFirstTime = false;
				errorCallBack.Invoke("Response timed out, please try again");
			}
		}
		static void UpdateProfile(string proposedName, Status status, Action<CharacterSettings> callBack,
			Action<string> errorCallBack)
		{

			Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
			{
				DisplayName = proposedName, //May be used for OOC chat, so find way to detect imposters
					PhotoUrl = null //TODO: set up later (user will eventually be able to update profile photo via the website)
			};

			Instance.auth.CurrentUser.UpdateUserProfileAsync(profile).ContinueWith(profileTask =>
			{
				if (profileTask.IsFaulted)
				{
					//The account will need to be removed from Auth records by an admin so user can try again: 
					//(maybe their internet dropped right at the moment of profile setting). 
					//Display name is critical so it needs to be set here and
					//account owners cannot destroy their own accounts.
					errorCallBack.Invoke("Major Error!: Please contact the dev team to help resolve it. " +
						profileTask.Exception.Message);
					Instance.isFirstTime = false;
					return;
				}

				Logger.LogFormat($"Firebase user created successfully: {Instance.auth.CurrentUser.DisplayName}",
					Category.DatabaseAPI);
			});
		}
	}
}