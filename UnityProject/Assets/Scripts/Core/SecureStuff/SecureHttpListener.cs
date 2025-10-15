using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace SecureStuff
{
	public static class SecureHttpListener
	{
		private static HttpListener listener;


		private static IReturnAndReceiveStringForSecureHttpListener _returnAndReceiveStringForSecureHttpListener;

		private static Dictionary<string, (int count, DateTime windowStart)> rateLimit = new Dictionary<string, (int, DateTime)>();
		private static int limit = 4; // Max requests
		private static TimeSpan window = TimeSpan.FromSeconds(60); // Time window


		public static void  StartHttpListener( IReturnAndReceiveStringForSecureHttpListener inIReturnAndReceiveStringForSecureHttpListener, int Port )
		{
			_returnAndReceiveStringForSecureHttpListener = inIReturnAndReceiveStringForSecureHttpListener;
			listener = new HttpListener();
			listener.Prefixes.Add($"http://*:{Port}/");
			listener.Start();

			Task.Run(ServerLoop);
		}

		private static async Task ServerLoop()
		{
			while (listener.IsListening)
			{
				try
				{
					var context = await listener.GetContextAsync(); // Waits for request

					var clientIp = context.Request.RemoteEndPoint.Address.ToString();
					var response = context.Response;
					if (AllowRequest(clientIp) == false)
					{
						response.OutputStream.Close();
						continue;
					}

					if (context.Request.HttpMethod == "GET")
					{
						string responseText = _returnAndReceiveStringForSecureHttpListener.GetReturnString();
						byte[] buffer = Encoding.UTF8.GetBytes(responseText);

						response.ContentType = "application/json";
						response.ContentLength64 = buffer.Length;
						await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
					}
					else if (context.Request.HttpMethod == "POST")
					{
						// Read POST Request
						using (var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
						{
							string body = await reader.ReadToEndAsync();
							var responseText = await _returnAndReceiveStringForSecureHttpListener.ReceiveString(body, clientIp);

							byte[] buffer = Encoding.UTF8.GetBytes(responseText);

							response.ContentType = "application/json";
							response.ContentLength64 = buffer.Length;
							await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
						}
					}

					response.OutputStream.Close();
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}
			}
		}

		private static bool AllowRequest(string ip)
		{


			// Proceed with rate limiting
			if (rateLimit.ContainsKey(ip) == false)
			{
				rateLimit[ip] = (1, DateTime.UtcNow);

				// Clean up expired entries
				var expiredKeys = rateLimit
					.Where(kvp => DateTime.UtcNow - kvp.Value.Item2 > window)
					.Select(kvp => kvp.Key)
					.ToList();

				foreach (var key in expiredKeys)
				{
					rateLimit.Remove(key);
				}
				return true;
			}

			var (count, windowStart) = rateLimit[ip];
			if (DateTime.UtcNow - windowStart > window)
			{
				// Reset window
				rateLimit[ip] = (1, DateTime.UtcNow);
				return true;
			}

			if (count >= limit)
				return false;

			rateLimit[ip] = (count + 1, windowStart);
			return true;
		}


		public static void OnApplicationQuit()
		{
			listener?.Stop();

		}
	}

	public interface IReturnAndReceiveStringForSecureHttpListener
	{
		public string GetReturnString();
		public Task<string> ReceiveString(string Data, string clientIp);
	}

}
