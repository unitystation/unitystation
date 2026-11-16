using System;
using US13.HealthV2.Living;

namespace US13.Mobs.BrainAI.ContextExtensions
{
	/// <summary>Inspector-selected template that creates working data owned by one brain.</summary>
	public interface IMobBehaviorContextExtension
	{
		IMobBehaviorContextExtension CreateRuntime();
		void Bind(MobBehaviorContext context, LivingHealthMasterBase health);
		void BeginRun();
		void EndRun();
		void Unbind();
	}

	[Serializable]
	public abstract class MobBehaviorContextExtension : IMobBehaviorContextExtension
	{
		protected MobBehaviorContext Context { get; private set; }
		public abstract IMobBehaviorContextExtension CreateRuntime();
		public virtual void Bind(MobBehaviorContext context, LivingHealthMasterBase health) => Context = context;
		public virtual void BeginRun() { }
		public virtual void EndRun() { }
		public virtual void Unbind() => Context = null;
	}

	public interface IMobFollowPolicy
	{
		bool CanFollowTarget { get; }
	}

	public interface IMobMovementFailureHandler
	{
		void OnMovementFailed(MobBehaviorContext context);
	}

	/// <summary>Declares the runtime context data an action needs, for editor validation.</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public sealed class RequiresMobContextExtensionAttribute : Attribute
	{
		public Type ExtensionType { get; }
		public RequiresMobContextExtensionAttribute(Type extensionType) => ExtensionType = extensionType;
	}
}
