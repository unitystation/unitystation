using System.Collections.Generic;

namespace US13.Managers.Supporters.FetchSupporterMethods
{
	public interface IFetchSupporters
	{
		public List<Supporter> FetchSupporters();
	}
}