using System;
using Firebase.Auth;

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
			try
			{
				FirebaseUser user = await FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(emailAcc, _password);

				await user.SendEmailVerificationAsync();

				UserProfile profile = new UserProfile
				{
					DisplayName = proposedName,
					PhotoUrl = null
				};

				await user.UpdateUserProfileAsync(profile);

				Logger.LogFormat($"Firebase user created successfully: {proposedName}",
					Category.DatabaseAPI);

				var newCharacter = new CharacterSettings
				{
					Name = StringManager.GetRandomMaleName(),
					Username = proposedName
				};

				callBack.Invoke(newCharacter);
			}
			catch (AggregateException ex)
			{
				var innerEx = ex.Flatten().InnerExceptions[0];
				Logger.LogError($"Failed to sign up {innerEx.Message}", Category.DatabaseAPI);
				errorCallBack.Invoke(innerEx.Message);
			}
			catch (Exception ex)
			{
				Logger.LogError($"Failed to sign up {ex.Message}", Category.DatabaseAPI);
				errorCallBack.Invoke(ex.Message);
			}
		}
	}
}
