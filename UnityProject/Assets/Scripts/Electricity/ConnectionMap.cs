using System;
using UnityEngine;

namespace Electricity{
	
	public static class ConnectionMap {
		/// <summary>
		/// Given the output position of a wire and an adjacent tile to check
		/// this method returns true if the adjacent tile has a connection 
		/// input point that is intersecting the origin tiles output point
		/// 
		/// originP = the two connection points of the WireTile that is doing the checking
		/// adjP = the two connection points of the WireTile that is being checked
		/// Adjtile = the direction to the adjacent tile
		/// </summary>
		public static bool IsConnectedToTile(ConnPoint originP, AdjDir adjTile, ConnPoint adjP){
			if(adjTile == AdjDir.SW){
				//SouthWest can't connect yet (future todo)
				return false;
			}
			if (adjTile == AdjDir.NW) { 
				//NorthWest can't connect yet (future todo)
				return false;
			}
			if (adjTile == AdjDir.NE) {
				//NorthEast can't connect yet (future todo)
				return false;
			}
			if (adjTile == AdjDir.SE) {
				//SouthEast can't connect yet (future todo)
				return false;
			}
			if(adjTile == AdjDir.N){
				if(originP.pointA == 1 || originP.pointB == 1){
					if(adjP.pointA == 2 || adjP.pointB == 2){
						return true;
					}
				}
			}
			if(adjTile == AdjDir.E){
				if(originP.pointA == 4 || originP.pointB == 4){
					if(adjP.pointA == 8 || adjP.pointB == 8){
						return true;
					}
				}
			}
			if (adjTile == AdjDir.S) {
				if (originP.pointA == 2 || originP.pointB == 2) {
					if (adjP.pointA == 1 || adjP.pointB == 1) {
						return true;
					}
				}
			}
			if (adjTile == AdjDir.W) {
				if (originP.pointA == 8 || originP.pointB == 8) {
					if (adjP.pointA == 4 || adjP.pointB == 4) {
						return true;
					}
				}
			}
			if (adjTile == AdjDir.Overlap) {
				if (originP.pointA == 0 || originP.pointB == 0 ||
				    originP.pointA == 1 && originP.pointB == 2 ||
				    originP.pointA == 2 && originP.pointB == 1 ||
				    originP.pointA == 4 && originP.pointB == 8 ||
				    originP.pointA == 8 && originP.pointB == 4) {
					if (adjP.pointA == 0 || adjP.pointB == 0) {
						return true;
					}
				}
			}

			return false;
		}
	}

	//Direction of the adjacent tile
	public enum AdjDir{
		SW, //Follows the iteratation of the FindPossibleConnection method
		W,  //in wire connect
		NW,
		S,
		Overlap,
		N,
		SE,
		E,
		NE
	}

	public struct ConnPoint{
		public int pointA;
		public int pointB;
	}
}
