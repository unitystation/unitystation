using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetPageSwitcher : MonoBehaviour
{
	public List<NetPage> Pages = new List<NetPage>();
	public NetPage DefaultPage;

	private void Start()
	{
		if ( Pages.Count == 0 )
		{
			Logger.LogWarningFormat( "{0}: Lazy ass didn't add any pages to the list, trying manually", Category.NetUI,	gameObject.name);
			Pages = this.GetComponentsOnlyInChildren<NetPage>().ToList();
		}

		if ( Pages.Count > 0 )
		{
			if ( !DefaultPage )
			{
				DefaultPage = Pages[0];
				Logger.LogWarningFormat( "{0}: Default Page not set, accepting first found from list ({1})", Category.NetUI,
					gameObject.name, DefaultPage );
			}
		}
	}
}