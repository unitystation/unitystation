using System;
using System.Net.Http;
using System.Text;
using UnityEngine;

namespace Core.Database
{
	[Serializable]
	internal class ApiDetailResponse : JsonObject
	{
		public string detail;
	}

	/// <summary>
	/// Parent class for all API request and response objects.
	/// </summary>
	public abstract class JsonObject
	{
		public virtual string ToJson()
		{
			return JsonUtility.ToJson(this);
		}

		public virtual StringContent ToStringContent()
		{
			return new StringContent(ToJson(), Encoding.UTF8, "application/json");
		}
	}

	/// <summary>
	/// Marks an API request as having or requiring an authentication token.
	/// </summary>
	public interface ITokenAuthable
	{
		string Token { get; }
	}
}
