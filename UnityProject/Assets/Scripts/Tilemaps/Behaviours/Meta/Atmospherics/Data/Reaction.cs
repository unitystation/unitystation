namespace Atmospherics
{
	public interface Reaction
	{
		bool Satisfies(GasMix gasMix);

		void React();
	}
}