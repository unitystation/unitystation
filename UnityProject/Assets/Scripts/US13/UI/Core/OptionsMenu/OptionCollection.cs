using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DynamicOptions
{
	public class OptionCollection
	{
		public Dictionary<Option, OptionItem> LivingOptionItems = new Dictionary<Option, OptionItem>();
		public GameObject Parent;
		public Dictionary<Option, OptionData> Options = new Dictionary<Option, OptionData>();


		public void OnValChange()
		{
			bool DoOrder = false;

			foreach (var Options in Options)
			{
				if (Options.Value.Show.Invoke())
				{
					if (LivingOptionItems.ContainsKey(Options.Key) == false)
					{
						LivingOptionItems[Options.Key] =
							OptionManager.Instance.InstantiateOptionItem(Options.Key, Options.Value, Parent, this);
						DoOrder = true;
					}
				}
				else
				{
					if (LivingOptionItems.ContainsKey(Options.Key))
					{
						Object.Destroy(LivingOptionItems[Options.Key].gameObject);
						LivingOptionItems.Remove(Options.Key);
						DoOrder = true;
					}
				}
			}

			if (DoOrder)
			{
				Order();
			}
		}

		public void Order()
		{
			// Sort living items by their enum value (underlying int)
			List<KeyValuePair<Option, OptionItem>> sorted = LivingOptionItems
				.OrderBy(kvp => (int) kvp.Key)
				.ToList();

			// Re-assign sibling indices to match sorted order
			for (int i = 0; i < sorted.Count; i++)
			{
				sorted[i].Value.transform.SetSiblingIndex(i);
			}
		}


		public void Destroy()
		{
			foreach (var o in LivingOptionItems)
			{
				Object.Destroy(o.Value.gameObject);
			}

			LivingOptionItems = null;
			Options = null;
		}


		public void ResetToDefault()
		{
			foreach (var o in LivingOptionItems)
			{
				o.Value.ResetPreference();
				Object.Destroy(o.Value.gameObject);
			}

			foreach (var Option in Options)
			{
				LivingOptionItems[Option.Key] =
					OptionManager.Instance.InstantiateOptionItem(Option.Key, Option.Value, Parent, this);
			}

			Order();
		}
	}
}