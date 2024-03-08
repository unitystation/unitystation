using System;
using System.Net.Http;
using NUnit.Framework;
using UnityEngine;
using Core.Database;

namespace Tests.Database
{
	public class ApiHandlerTests
	{
		private static readonly Uri mockUri = new Uri("https://example.com/fake/uri");

		[Test]
		public void Get_Send()
		{
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient(new MockHttpServer((request) =>
			{
				Assert.AreEqual(mockUri, request.RequestUri);

				return new HttpResponseMessage();
			}));

			ApiServer.Get<JsonObject>(mockUri).Wait();
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient();
		}

		[Test]
		public void Get_SendAuth()
		{
			var mockToken = "asdf123faketokenhuehuehue";
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient(new MockHttpServer((request) =>
			{
				Assert.AreEqual("Token", request.Headers.Authorization.Scheme);
				Assert.AreEqual(mockToken, request.Headers.Authorization.Parameter);

				return new HttpResponseMessage();
			}));

			ApiServer.Get<JsonObject>(mockUri, mockToken).Wait();
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient();
		}

		[Test]
		public void Get_ReceiveSuccess()
		{
			var mockResponse = new MockResponse();
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient(new MockHttpServer((request) =>
			{
				return new HttpResponseMessage()
				{
					Content = mockResponse.ToStringContent(),
				};
			}));

			var task = ApiServer.Get<MockResponse>(mockUri);
			task.Wait();
			var response = task.Result;

			Assert.AreEqual(mockResponse.receive_string, response.Data?.receive_string);
			Assert.AreEqual(mockResponse.receive_integer, response.Data?.receive_integer);
			Assert.AreEqual(mockResponse.receive_object.nested_field, response.Data.receive_object.nested_field);
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient();
		}

		[Test]
		public void Post_Send()
		{
			var mockRequest = new MockRequest();

			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient(new MockHttpServer((request) =>
			{
				var task = request.Content.ReadAsStringAsync();
				task.Wait();
				var requestBody = task.Result;
				var requestObject = JsonUtility.FromJson<MockRequest>(requestBody);

				Assert.AreEqual(mockUri, request.RequestUri);
				Assert.AreEqual(mockRequest.send_string, requestObject.send_string);
				Assert.AreEqual(mockRequest.send_integer, requestObject.send_integer);
				Assert.AreEqual(mockRequest.send_object.nested_field, requestObject.send_object.nested_field);

				return new HttpResponseMessage();
			}));

			ApiServer.Post<JsonObject>(mockUri, mockRequest).Wait();
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient();
		}

		[Test]
		public void Post_SendAuth()
		{
			var mockRequest = new MockRequestAuth();
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient(new MockHttpServer((request) =>
			{
				Assert.AreEqual("Token", request.Headers.Authorization.Scheme);
				Assert.AreEqual(mockRequest.Token, request.Headers.Authorization.Parameter);

				return new HttpResponseMessage();
			}));

			ApiServer.Post<JsonObject>(mockUri, mockRequest).Wait();
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient();
		}

		[Test]
		public void Post_ReceiveSuccess()
		{
			var mockResponse = new MockResponse();
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient(new MockHttpServer((request) =>
			{
				return new HttpResponseMessage()
				{
					Content = mockResponse.ToStringContent(),
				};
			}));

			var task = ApiServer.Post<MockResponse>(mockUri, new MockRequest());
			task.Wait();
			var response = task.Result;

			Assert.AreEqual(mockResponse.receive_string, response.Data?.receive_string);
			Assert.AreEqual(mockResponse.receive_integer, response.Data?.receive_integer);
			Assert.AreEqual(mockResponse.receive_object.nested_field, response.Data.receive_object.nested_field);
			SecureStuff.SafeHttpRequest.EditorOnlySet = new HttpClient();
		}

		#region Helper Request, Response Models

		[Serializable]
		private class MockRequest : JsonObject
		{
			public string send_string = "send string";
			public int send_integer = 123;
			public MockNestedObject send_object = new MockNestedObject();
		}

		[Serializable]
		private class MockRequestAuth : JsonObject, ITokenAuthable
		{
			public string Token => "asdf123faketokenhuehuehue";
		}

		[Serializable]
		private class MockResponse : JsonObject
		{
			public string receive_string = "receive string";
			public int receive_integer = 789;
			public MockNestedObject receive_object = new MockNestedObject();
		}

		[Serializable]
		private class MockNestedObject : JsonObject
		{
			public string nested_field = "nested field";
		}

		#endregion
	}
}
