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

		    closet.closetType = type;
		    closet.Passable = false;
		    closet.IsClosed = isClosed;

		    return closet;
	    }

	    private static readonly object[] Cases =
	    {
		    new object[] {ClosetType.LOCKER, true, true, true, false},
		    new object[] {ClosetType.LOCKER, true, false, false, true},
		    new object[] {ClosetType.LOCKER, false, true, true, false},
		    new object[] {ClosetType.LOCKER, false, false, false, true},
		    new object[] {ClosetType.CRATE, true, false, true, false},
		    new object[] {ClosetType.CRATE, false, true, true, false},
		    new object[] {ClosetType.CRATE, true, true, true, false},
		    new object[] {ClosetType.CRATE, false, false, true, false}
	    };

	    private static void AssertCollidersEnabled(RegisterCloset closet, bool shouldEnableColliders)
	    {
		    foreach (var collider in closet.GetComponents<Collider2D>())
		    {
			    Assert.AreEqual(collider.enabled, shouldEnableColliders);
		    }
	    }

        [TestCaseSource(nameof(Cases))]
        public void GivenClosetOfTypeAndOpenCloseState_WhenOpenOrClose_ThenHasCorrectCollidersAndPassabilityStatus(ClosetType type, bool isInitiallyClosed, bool thenIsClosed, bool shouldEnableColliders, bool shouldBePassable)
        {
	        var closet = GetRegisterCloset(type, isInitiallyClosed);
	        if (isInitiallyClosed == thenIsClosed)
	        {
		        //check right away
		        AssertCollidersEnabled(closet, shouldEnableColliders);
		        Assert.AreEqual(closet.Passable, shouldBePassable);
	        }
	        else
	        {
		        //change and check
		        closet.IsClosed = thenIsClosed;
		        AssertCollidersEnabled(closet, shouldEnableColliders);
		        Assert.AreEqual(closet.Passable, shouldBePassable);
	        }
        }
    }
}
