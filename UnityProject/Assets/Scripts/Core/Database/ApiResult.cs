﻿using System.Net;
using JetBrains.Annotations;

namespace Core.Database
{
	public class ApiResult<T>: JsonObject where T : JsonObject
	{
		public HttpStatusCode StatusCode { get;  init; }
		[CanBeNull] public T Data { get;  init; }
		[CanBeNull] public ApiHttpException Exception { get;  init; }

		public bool IsSuccess => Exception == null;

		private ApiResult(HttpStatusCode statusCode, T data, ApiHttpException exception = null)
		{
			StatusCode = statusCode;
			Data = data;
			Exception = exception;
		}

		public static ApiResult<T> Success(HttpStatusCode statusCode, T data) => new(statusCode, data);
		public static ApiResult<T> Failure(HttpStatusCode statusCode, T data, ApiHttpException exception) => new(statusCode, data, exception);
	}
}