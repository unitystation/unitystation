using System.Collections.Generic;

namespace Core.Highlight
{
	public interface IHighlightable
	{
		public List<string>  SearchableString();
		public void HighlightObject();
	}
}