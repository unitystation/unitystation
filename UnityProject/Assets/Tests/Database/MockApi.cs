using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Database
{
	/// <summary>
	/// A mock HTTP message handler for testing the database API wrapper.
	/// Used as a parameter for <see cref="HttpClient"/>.
	/// </summary>
	public class MockHttpServer : HttpMessageHandler
	{
		private Func<HttpRequestMessage, HttpResponseMessage> testAction;

		/// <param name="action">
		///	The action the test should supply.
		///	Takes a <see cref="HttpRequestMessage"/> parameter should return a <see cref="HttpResponseMessage"/>
		///	</param>
		public MockHttpServer(Func<HttpRequestMessage, HttpResponseMessage> action)
		{
			testAction = action;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
		{
			var response = testAction?.Invoke(request);
			response.Content ??= new StringContent(string.Empty);

			var taskSource = new TaskCompletionSource<HttpResponseMessage>();
			taskSource.SetResult(response);
			return taskSource.Task;
		}
	}
}
