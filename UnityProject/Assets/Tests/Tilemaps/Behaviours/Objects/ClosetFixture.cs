using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Tilemaps.Behaviours.Objects
{
    public class ClosetFixture
    {
	    private static RegisterCloset GetRegisterCloset(ClosetType type, bool isClosed)
	    {
		    GameObject obj = new GameObject();
		    obj.AddComponent<BoxCollider2D>();
		    obj.AddComponent<BoxCollider2D>();
		    var closet = obj.AddComponent<RegisterCloset>();
		    //always starts in machines layer
		    obj.layer = LayerMask.NameToLayer("Machines");

		    closet.closetType = type;
		    closet.Passable = false;
		    closet.IsClosed = isClosed;

		    return closet;
	    }

	    private static readonly object[] Cases =
	    {
		    new object[] {ClosetType.LOCKER, true, true, LayerMask.NameToLayer("Machines"), false},
		    new object[] {ClosetType.LOCKER, true, false, LayerMask.NameToLayer("Items"), true},
		    new object[] {ClosetType.LOCKER, false, true, LayerMask.NameToLayer("Machines"), false},
		    new object[] {ClosetType.LOCKER, false, false, LayerMask.NameToLayer("Items"), true},
		    new object[] {ClosetType.CRATE, true, false, LayerMask.NameToLayer("Machines"), false},
		    new object[] {ClosetType.CRATE, false, true, LayerMask.NameToLayer("Machines"), false},
		    new object[] {ClosetType.CRATE, true, true, LayerMask.NameToLayer("Machines"), false},
		    new object[] {ClosetType.CRATE, false, false, LayerMask.NameToLayer("Machines"), false}
	    };

	    private static void AssertCollidersEnabled(RegisterCloset closet, bool shouldEnableColliders)
	    {
		    foreach (var collider in closet.GetComponents<Collider2D>())
		    {
			    Assert.AreEqual(collider.enabled, shouldEnableColliders);
		    }
	    }

        [TestCaseSource(nameof(Cases))]
        public void GivenClosetOfTypeAndOpenCloseState_WhenOpenOrClose_ThenHasCorrectLayerAndPassabilityStatus(ClosetType type, bool isInitiallyClosed, bool thenIsClosed, int shouldBeInLayer, bool shouldBePassable)
        {
	        var closet = GetRegisterCloset(type, isInitiallyClosed);
	        if (isInitiallyClosed == thenIsClosed)
	        {
		        //check right away
		        Assert.AreEqual(closet.gameObject.layer, shouldBeInLayer);
		        Assert.AreEqual(closet.Passable, shouldBePassable);
	        }
	        else
	        {
		        //change and check
		        closet.IsClosed = thenIsClosed;
		        Assert.AreEqual(closet.gameObject.layer, shouldBeInLayer);
		        Assert.AreEqual(closet.Passable, shouldBePassable);
	        }

	        //collider should always be one no matter what
	        AssertCollidersEnabled(closet, true);
        }
    }
}
