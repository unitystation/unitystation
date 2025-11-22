using System.Collections.Generic;

namespace Managers.Supporters.FetchSupporterMethods
{
	public interface IFetchSupporters
	{
		public List<Supporter> FetchSupporters();
	}
}