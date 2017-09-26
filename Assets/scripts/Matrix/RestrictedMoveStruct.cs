using UnityEngine;
/// <summary>
/// Flags to determine in which direction player is
/// allowed to leave when standing on a restricted movement tile
/// </summary>
public struct RestrictedMoveStruct {
	public bool north;
	public bool south;
	public bool east;
	public bool west;

	/// <summary>
	/// False means the movement is allowed
	/// </summary>
	public bool CheckAllowedDir(Vector3 dir){
		if(dir == Vector3.up && !north){
			return true;
		}
		if (dir == Vector3.down && !south) {
			return true;
		}
		if (dir == Vector3.right && !east) {
			return true;
		}
		if (dir == Vector3.left && !west) {
			return true;
		}

		return false;
	}
}
