using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Core.Networking;
using Logs;

[assembly: InternalsVisibleTo("Tests")]
namespace Core.Database
{
	/// <summary>
	/// HTTP wrapper for database API requests.
	/// </summary>
	public static class ApiServer
	{
		internal static async Task<T> Get<T>(Uri uri, string token = default) where T : JsonObject
		{
			var responseBody = await Get(uri, token);
			return JsonConvert.DeserializeObject<T>(responseBody);
		}

		internal static async Task<T> Post<T>(Uri uri, JsonObject body) where T : JsonObject
		{
			try
			{
				var responseBody = await Post(uri, body);
				var response = JsonConvert.DeserializeObject<T>(responseBody);
				return response;
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
				return null;
			}
		}


		internal static async Task<string> Get(Uri uri, string token = default)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, uri);

			if (token != default)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
			}

			return await Send(request);
		}

		internal static async Task<string> Post(Uri uri, JsonObject body)
		{
			try
			{
				var request = new HttpRequestMessage(HttpMethod.Post, uri);

				if (body is ITokenAuthable authable)
				{
					request.Headers.Authorization = new AuthenticationHeaderValue("Token", authable.Token);
				}

				var sss = JsonConvert.SerializeObject(body);
				request.Content = new StringContent(sss, Encoding.UTF8, "application/json");
				//request.Content = body.ToStringContent();
				Loggy.Log("await Send(request);");
				return await Send(request);

			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
				return null;
			}
		}


		private static async Task<string> Send(HttpRequestMessage request)
		{
			request.Headers.Add("Accept", "application/json");
			Loggy.Log("SecureStuff.SafeHttpRequest.SendAsync");
			HttpResponseMessage response = await SecureStuff.SafeHttpRequest.SendAsync(request);
			var responseBody = await response.Content.ReadAsStringAsync();

			Loggy.Log(responseBody);
			if (response.IsSuccessStatusCode == false)
			{
				if (TryGetApiRequestException(responseBody, out var requestException))
				{
					throw requestException;
				}
				else
				{
					throw new ApiHttpException(response.ReasonPhrase, response.StatusCode);
				}
			}

			return responseBody;
		}

		/// <summary>
		/// Attempts to get any usage-related errors from the given API server's response and
		/// provides an unthrown <see cref="ApiRequestException"/> instance if any are found.
		/// </summary>
		/// <param name="response">Response body to test</param>
		/// <param name="requestException">An <see cref="ApiRequestException"/>  or null</param>
		/// <returns>True if an API error found</returns>
		private static bool TryGetApiRequestException(string response, out ApiRequestException requestException)
		{
			requestException = null;

			// First check for odd response (malformed JSON of a Detail error from the server?)
			if (TryParseMalformedApiDetailResponse(response, out var message))
			{
				requestException = new ApiRequestException(message);
				requestException.Messages.Add(message);
				return true;
			}

			// Else try get an API detail response.
			var detailResponse = JsonConvert.DeserializeObject<ApiDetailResponse>(response);
			if (string.IsNullOrEmpty(detailResponse?.detail) == false)
			{
				requestException = new ApiRequestException(detailResponse.detail);
				requestException.Messages.Add(detailResponse.detail);
				return true;
			}

			// Else try get an API error response.
			if (TryGetApiErrorResponse(response, out var errorCollections))
			{
				requestException = new ApiRequestException(errorCollections?.Values.First().First());

				foreach (string[] errors in errorCollections?.Values)
				{
					requestException.Messages.Concat(errors);
				}
				return true;
			}

			return false;
		}

		private static bool TryParseMalformedApiDetailResponse(string response, out string message)
		{
			message = string.Empty;

			var startMarker = "[ErrorDetail(string=\'";

			int from = response.IndexOf(startMarker) + startMarker.Length;
			if (from <= startMarker.Length) return false;

			int to = response.IndexOf("\'", from);
			if (to < 0) return false;

			message = response[from..to];

			return true;
		}

		private static bool TryGetApiErrorResponse(string response, out Dictionary<string, string[]> errorCollections)
		{
			errorCollections = default;

			// This parser is slower but Unity doesn't want to deserialize nested collections.
			var jObject = JsonConvert.DeserializeObject<JObject>(response);
			if (jObject != null && jObject.TryGetValue("error", out JToken jErrors))
			{
				errorCollections = jErrors.ToObject<Dictionary<string, string[]>>();
				return true;
			}

			return false;
		}
	}
}
