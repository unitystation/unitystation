using System.Collections.Generic;
using System.Linq;
using Logs;
using Mirror;
using Shared.Managers;
using UnityEngine;

namespace Actions.V2.UI
{
    public class ActionButtonManager : SingletonManager<ActionButtonManager>
    {
        public GameObject ActionButtonPrefab;

        private List<UIActionButton> spawnedButtonsBody = new List<UIActionButton>();
        private List<UIActionButton> spawnedButtonsMind = new List<UIActionButton>();

        public void RefreshButtonsBody(SyncList<ActionButtonData> actionButtons, NetworkIdentity owner)
	        => RefreshButtons(actionButtons, spawnedButtonsBody, owner);

        public void RefreshButtonsMind(SyncList<ActionButtonData> actionButtons, NetworkIdentity owner)
	        => RefreshButtons(actionButtons, spawnedButtonsMind, owner, true);

        private void RefreshButtons(SyncList<ActionButtonData> actionButtons, List<UIActionButton> spawnedButtons, NetworkIdentity owner, bool isMindAction = false)
        {
            var trackedItems = new List<string>();

            foreach (var buttonData in actionButtons)
            {
	            if (buttonData == null || !PlayerManager.LocalMindScript) continue;
                if (buttonData.TrackingObject &&
                    (PlayerManager.LocalMindScript.netIdentity.netId != buttonData.TrackingObject.netId
                     && PlayerManager.LocalMindScript.GetDeepestBody().netId != buttonData.TrackingObject.netId))
                {
					continue;
                }

                trackedItems.Add(buttonData.ID);
                if (spawnedButtons.Exists(b => b.name == buttonData.ID))
                {
                    continue;
                }

                var newButton = Instantiate(ActionButtonPrefab, transform);
                var logic = newButton.GetComponent<UIActionButton>();
	            logic?.Setup(buttonData, isMindAction, owner);
                spawnedButtons.Add(logic);
            }

            spawnedButtons.RemoveAll(x =>
            {
                var shouldRemove = trackedItems.Contains(x.ActionData.ID) == false;
                if (shouldRemove)
                {
                    Destroy(x.gameObject);
                }
                return shouldRemove;
            });
        }

        public void DeleteSpawnedBodyUIActions(NetworkIdentity ownerRequest)
        {
	        foreach (var button in spawnedButtonsBody.ToList())
	        {
		        if (button.Owner != ownerRequest) continue;
		        if (button != null)
		        {
			        spawnedButtonsBody.Remove(button);
			        Destroy(button.gameObject);
		        }
	        }
        }

        public void DeleteSpawnedMindUIActions(NetworkIdentity ownerRequest)
		{
	        foreach (var button in spawnedButtonsMind.ToList())
	        {
		        if (button.Owner != ownerRequest) continue;
		        if (button != null)
		        {
			        spawnedButtonsMind.Remove(button);
			        Destroy(button.gameObject);
		        }
	        }
		}
    }
}