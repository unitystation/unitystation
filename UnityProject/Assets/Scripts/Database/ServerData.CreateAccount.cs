using System;

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
			Instance.isFirstTime = true;
			Instance.auth.CreateUserWithEmailAndPasswordAsync(emailAcc, _password).ContinueWith(task =>
			{
				if (task.IsCanceled)
				{
					errorCallBack.Invoke("Cancelled");
					Instance.isFirstTime = false;
					return;
				}
				if (task.IsFaulted)
				{
					errorCallBack.Invoke(task.Exception.Message);
					Instance.isFirstTime = false;
					return;
				}

				// Firebase user has been created.
				Firebase.Auth.FirebaseUser newUser = task.Result;

				Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
				{
					DisplayName = proposedName, //May be used for OOC chat, so find way to detect imposters
						PhotoUrl = null //TODO: set up later (user will eventually be able to update profile photo via the website)
				};

				newUser.UpdateUserProfileAsync(profile);

				Logger.LogFormat($"Firebase user created successfully: {newUser.DisplayName}",
					Category.DatabaseAPI);

				var newCharacter = new CharacterSettings();
				callBack.Invoke(newUser, newCharacter);
			});
		}
	}
}