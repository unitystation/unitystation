using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blob
{
	public class BlobStructure : MonoBehaviour
	{
		public bool isCore;
		public bool isNode;
		public bool isResource;
		public bool isFactory;
		public bool isReflective;
		public bool isStrong;
		public bool isNormal;

		private Integrity integrity;

		private void Start()
		{
			integrity = GetComponent<Integrity>();
		}
	}
}
