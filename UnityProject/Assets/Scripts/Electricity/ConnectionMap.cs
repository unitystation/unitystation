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
				//SouthWest can never connect
				return false;
			}
			if (adjTile == AdjDir.NW) { 
				//NorthWest can never connect
				return false;
			}
			if (adjTile == AdjDir.NE) {
				//NorthEast can never connect
				return false;
			}
			if (adjTile == AdjDir.SE) {
				//SouthEast can never connect
				return false;
			}
			if(adjTile == AdjDir.N){
				if(originP.pointA == 1 || originP.pointB == 1){
					if(adjP.pointA == 2 || adjP.pointB == 2){
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
