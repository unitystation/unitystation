using System.Collections.Generic;

namespace HealthV2.Living.CirculatorySystem
{
	/// <summary>
	/// Describes the area a Metabolism Reaction can happen over , This is needed because surface reactions are funny and surface medicine is funny to
	/// </summary>
	public interface IAreaReactionBase
	{
		public List<MetabolismReaction> MetabolismReactions { get; }
	}
}
