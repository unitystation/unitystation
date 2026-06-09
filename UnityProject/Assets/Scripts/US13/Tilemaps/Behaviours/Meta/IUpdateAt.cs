using UnityEngine;

namespace US13.Tilemaps.Behaviours.Meta
{
	public interface IUpdateAt
	{
		public SystemType SubsystemType { get; }
		public void UpdateAt(Vector3Int localPosition);
	}
}
