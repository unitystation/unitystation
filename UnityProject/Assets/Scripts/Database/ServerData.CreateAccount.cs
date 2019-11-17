using System;
using System.Collections;
using Firebase.Auth;
using Firebase;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		///<summary>
		///Tries to create an account for the user in player accounts (now using firebase as of nov '19)
		///</summary>
		public async static void TryCreateAccount(string proposedName, string _password, string emailAcc,
			Action<CharacterSettings> callBack, Action<string> errorCallBack)
		{
			FirebaseUser user;
			try
			{
				user = await FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(emailAcc, _password);
			}
			catch (FirebaseException e)
			{
				Logger.LogError($"Failed to sign up {e.Message}");
				errorCallBack.Invoke(e.Message);
				return;
			}

			await user.SendEmailVerificationAsync();

			UserProfile profile = new UserProfile
			{
				DisplayName = proposedName,
				PhotoUrl = null
			};

			await user.UpdateUserProfileAsync(profile);

			Logger.LogFormat($"Firebase user created successfully: {proposedName}",
				Category.DatabaseAPI);

			var newCharacter = new CharacterSettings();
			newCharacter.Name = StringManager.GetRandomMaleName();
			newCharacter.username = proposedName;
			callBack.Invoke(newCharacter);
		}
	}
}