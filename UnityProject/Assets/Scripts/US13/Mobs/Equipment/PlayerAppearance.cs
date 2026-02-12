using UnityEngine;
using US13.Items;
using US13.Player;
using US13.Systems.Inventory;

namespace US13.Mobs.Equipment
{
	public static class PlayerAppearance
	{
		public static void Process(GameObject equipmentObject, int index, GameObject _Item,
			bool _forceInit = false, bool _isBodyParts = false)
		{
			if (equipmentObject != null)
			{
				if (_isBodyParts == false)
				{
					ClothingItem c = equipmentObject.GetComponent<Equipment>().GetClothingItem((NamedSlot) index);
					if (_Item == null)
					{
						if (_forceInit == false) c.SetReference(null);
					}
					else
					{
						c.SetReference(_Item);
					}

					if (_forceInit) c.PushTexture();
				}
				else
				{
					ClothingItem c = equipmentObject.GetComponent<PlayerSprites>().characterSprites[index];
					if (_Item == null)
					{
						if (_forceInit == false) c.SetReference(null);
					}
					else
					{
						c.SetReference(_Item);
					}

					if (_forceInit) c.PushTexture();
				}
			}
		}
	}
}
