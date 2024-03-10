using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JetBrains.Annotations;
using Logs;

[assembly: InternalsVisibleTo("Tests")]
namespace Core.Database
{
	/// <summary>
	/// HTTP wrapper for database API requests.
	/// </summary>
	public static class ApiServer
	{
		internal static async Task<ApiResult<T>> Get<T>(Uri uri, string token = default) where T : JsonObject
		{
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

			if (token != default)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
			}

			return await Send<T>(request);
		}

		internal static async Task<ApiResult<T>> Post<T>(Uri uri, JsonObject body, string token = default) where T : JsonObject
		{
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);

			if (body is ITokenAuthable authable)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Token", authable.Token);
			}

			if (token != default)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
			}

			string sss = JsonConvert.SerializeObject(body);
			request.Content = new StringContent(sss, Encoding.UTF8, "application/json");
			return await Send<T>(request);
		}

		internal static async Task<ApiResult<T>> Put<T>(Uri uri, JsonObject body, string token = default) where T : JsonObject
		{
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);

			if (body is ITokenAuthable authable)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Token", authable.Token);
			}

			if (token != default)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
			}

			var sss = JsonConvert.SerializeObject(body);
			request.Content = new StringContent(sss, Encoding.UTF8, "application/json");
			//request.Content = body.ToStringContent();
			Loggy.Log("await Send(request);");
			return await Send<T>(request);
		}

		internal static async Task<ApiResult<T>> Delete<T>(Uri uri, string token = default) where T : JsonObject
		{
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri);

			if (token != default)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
			}

			return await Send<T>(request);
		}


		private static async Task<ApiResult<T>> Send<T>(HttpRequestMessage request) where T : JsonObject
		{
			request.Headers.Add("Accept", "application/json");
			HttpResponseMessage response = await SecureStuff.SafeHttpRequest.SendAsync(request);
			string responseBody = await response.Content.ReadAsStringAsync();

			Loggy.Log(responseBody);
			if (response.IsSuccessStatusCode == false)
			{
				if (TryGetApiRequestException(responseBody, response.StatusCode, out ApiRequestException requestException))
				{
					return ApiResult<T>.Failure(response.StatusCode, null, requestException);
				}

				return ApiResult<T>.Failure(response.StatusCode, null,
					new ApiHttpException(response.ReasonPhrase, response.StatusCode));
			}

			return ApiResult<T>.Success(response.StatusCode, JsonConvert.DeserializeObject<T>(responseBody));
		}

		/// <summary>
		/// Attempts to get any usage-related errors from the given API server's response and
		/// provides an unthrown <see cref="ApiRequestException"/> instance if any are found.
		/// </summary>
		/// <param name="response">Response body to test</param>
		/// <param name="statusCode"></param>
		/// <param name="requestException">An <see cref="ApiRequestException"/>  or null</param>
		/// <returns>True if an API error found</returns>
		private static bool TryGetApiRequestException(string response, HttpStatusCode statusCode, [CanBeNull] out ApiRequestException requestException)
		{
			dynamic errorResponse = JsonConvert.DeserializeObject<dynamic>(response);
			ApiRequestException tempException = new("An error occurred", statusCode);
			requestException = null;

			switch (errorResponse.error)
			{
				case JValue:
					HandleSimpleError(tempException, errorResponse);
					requestException = tempException;
					break;
				case JObject:
					HandleComplexError(tempException, errorResponse);
					requestException = tempException;
					break;
			}

			return requestException is not null && requestException.Messages.Any();
		}

		private static void HandleComplexError(ApiRequestException requestException, dynamic errorResponse)
		{
			foreach (var prop in errorResponse.error)
			{
				foreach (var message in prop.Value)
				{
					requestException.Messages.Add(message.ToString());
				}
			}
		}

		private static void HandleSimpleError(ApiRequestException requestException, dynamic errorResponse)
		{
			requestException.Messages.Add(errorResponse.error.ToString());
		}
	}
}
