using System.Collections.Generic;

namespace US13.Core.Highlight
{
	public interface IHighlightable
	{
		public List<string>  SearchableString();
		public void HighlightObject();
	}
}