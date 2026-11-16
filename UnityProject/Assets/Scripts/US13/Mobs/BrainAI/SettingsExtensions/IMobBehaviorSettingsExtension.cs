using System;

namespace US13.Mobs.BrainAI.SettingsExtensions
{
	/// <summary>Inspector-selected configuration owned by a brain's serialized behavior.</summary>
	public interface IMobBehaviorSettingsExtension { }

	/// <summary>Supplies a graph interval without coupling shared timing actions to a particular mob.</summary>
	public interface IMobBehaviorIntervalSettings : IMobBehaviorSettingsExtension
	{
		int IntervalId { get; }
		float IntervalSeconds { get; }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public sealed class RequiresMobSettingsAttribute : Attribute
	{
		public Type SettingsType { get; }
		public RequiresMobSettingsAttribute(Type settingsType) => SettingsType = settingsType;
	}
}
