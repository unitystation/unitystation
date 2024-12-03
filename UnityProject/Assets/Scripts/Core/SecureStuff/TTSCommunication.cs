using System.Net.Http;
using UnityEngine;

namespace SecureStuff
{
	public static class TTSCommunication
	{

		private HttpClient Client = new HttpClient();

		public static byte[] GenTTS(string Input, string voice)
		{
			try
			{
				HttpResponseMessage response = await SafeHttpRequest.GetAsync(GetURL(textToSynth));

				if (response.IsSuccessStatusCode == false)
				{
					Loggy.Error("Err: " + response.ReasonPhrase);
				}
				else
				{
					byte[] responseData = await response.Content.ReadAsByteArrayAsync();
					LoadManager.DoInMainThread(() => { callback.Invoke(responseData); });
				}
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}
		}




	}
}