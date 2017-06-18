﻿using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UI;

public class ClothingRegressionTest {
	GameObject obj;
	ItemAttributes subject;
	
	[SetUp]
	public void setUp()
	{
		obj = new GameObject("Item");
		obj.SetActive(false);
		
		var sprite = new GameObject("Sprite");
		sprite.AddComponent<SpriteRenderer>();
		
		sprite.transform.SetParent(obj.transform); //is this how you add a child?
		
		subject = obj.AddComponent<ItemAttributes>();
		subject.runInEditMode = true;
		subject.spriteType = SpriteType.Clothing;
	}

	[Test]
	public void Gloves_Should_Have_Clothing_Offset() {
		subject.hierarchy = "/obj/item/clothing/gloves/color/green";
		refreshItem();
		Assert.That(subject.clothingReference != -1);
	}
	[Test]
	public void GeneticsSuit_Should_Have_Offsets() {
		subject.hierarchy = "/obj/item/clothing/under/rank/geneticist";
		refreshItem();
		Assert.That(subject.clothingReference != -1);
		Assert.That(subject.inHandReferenceLeft != -1);
		Assert.That(subject.inHandReferenceRight != -1);
	}

	void refreshItem()
	{
		obj.SetActive(false);
		obj.SetActive(true);
		Debug.LogFormat("{0}: c={1}, l={2}, r={3}",subject.itemName, subject.clothingReference, subject.inHandReferenceLeft, subject.inHandReferenceRight);

	}

}
