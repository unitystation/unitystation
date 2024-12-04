using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Logs;
using Newtonsoft.Json;
using UnityEngine;

namespace SecureStuff
{
	public static class TTSCommunication
	{
		private class DataTSS
		{
			public string input_string { get; set; }
			public string voice { get; set; }
		}

		public static async Task<byte[]> GenTTS(string Input, string voice)
		{
			// URL of the API endpoint
			string url = "http://127.0.0.1:5234/generate-audio";

			// Create the JSON payload
			DataTSS payload = new DataTSS()
			{
				input_string = Input,
				voice = voice
			};

			// Serialize payload to JSON
			string jsonPayload = JsonConvert.SerializeObject(payload);

			// Initialize HttpClient
			using (HttpClient client = new HttpClient())
			{
				try
				{
					// Create content with JSON payload
					var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

					// Send POST request
					HttpResponseMessage response = await client.PostAsync(url, content);

					// Ensure the request was successful
					response.EnsureSuccessStatusCode();

					// Read the audio file from the response
					byte[] audioData = await response.Content.ReadAsByteArrayAsync();

					return audioData;
				}
				catch (Exception ex)
				{
					Loggy.Warning($"Error: {ex.Message}");
				}
			}

			return null;
		}

	}
}