using System;
using Firebase.Auth;
using Logs;

namespace DatabaseAPI
{
	public partial class ServerData
	{
		///<summary>
		///Tries to create an account for the user in player accounts (now using firebase as of nov '19)
		///</summary>
		public async static void TryCreateAccount(string proposedName, string _password, string emailAcc,
			Action<FirebaseUser> callBack, Action<string> errorCallBack)
		{
			try
			{
				FirebaseUser user = await FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(emailAcc, _password);

				await user.SendEmailVerificationAsync();

				var profile = new UserProfile
				{
					DisplayName = proposedName,
					PhotoUrl = null
				};

				await user.UpdateUserProfileAsync(profile);

				Loggy.LogFormat($"Firebase user created successfully: {proposedName}",
					Category.DatabaseAPI);

				callBack.Invoke(user);
			}
			catch (AggregateException ex)
			{
				var innerEx = ex.Flatten().InnerExceptions[0];
				Loggy.LogError($"Failed to sign up: {innerEx.Message}", Category.DatabaseAPI);
				errorCallBack.Invoke(innerEx.Message);
			}
			catch (Exception ex)
			{
				Loggy.LogError($"Failed to sign up: {ex.Message}", Category.DatabaseAPI);
				errorCallBack.Invoke(ex.Message);
			}
		}
	}
}
