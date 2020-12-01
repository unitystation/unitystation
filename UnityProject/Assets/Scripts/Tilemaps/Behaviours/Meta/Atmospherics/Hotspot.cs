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

		public bool Process()
		{
			if (PlasmaFireReaction.CanHoldHotspot(node.GasMix))
			{
				Reactions.React(node.GasMix);
				return true;
			}
			return false;
		}
	}
}