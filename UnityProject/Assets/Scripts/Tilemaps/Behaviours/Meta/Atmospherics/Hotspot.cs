namespace Systems.Atmospherics
{
	/// <summary>
	/// Represents the potential for a MetaDataNode to ignite the gases on it, and provides logic related to igniting the actual
	/// gases.
	/// </summary>
	public class Hotspot
	{
		/// <summary>
		/// Node this hotspot lives on.
		/// </summary>
		public MetaDataNode node;

		public Hotspot(MetaDataNode newNode)
		{
			node = newNode;
		}

		public void Process()
		{
			Reactions.React(node.GasMix, node.PositionMatrix);
		}
	}
}