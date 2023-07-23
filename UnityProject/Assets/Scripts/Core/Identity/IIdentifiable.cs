﻿namespace Core.Identity
{
	/// <summary>
	/// <seealso cref="IIdentifiable"/>
	/// Provides a mechanism to identify game entities in a flexible manner.
	/// Entities implementing this interface provide their current display name,
	/// which could be influenced by conditions such as disguise or visibility.
	/// </summary>
	public interface IIdentifiable
	{
		/// <summary>
		/// Current name of this entity. This might change over time due to disguises, visibility, labelling, etc.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// The absolute initial name of this entity. This will never change. Useful to try to identify an object by its initial name.
		/// or get the real name of a character.
		/// </summary>
		public string InitialName { get; }

		/// <summary>
		/// Server only. Sets the display name of this entity. Make sure to add the [Server] attribute to your implementation.
		/// </summary>
		/// <param name="oldName"></param>
		/// <param name="newName"></param>
		public void SyncDisplayName(string oldName, string newName);
	}
}