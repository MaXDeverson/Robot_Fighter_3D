using UnityEngine;

public class TilingFloorTrigger : MonoBehaviour {

	public float MoveOffset;
	public DIRECTION MoveDirection = DIRECTION.Right;
	public GameObject ObjectToMove;

	void OnTriggerEnter(){
		if(ObjectToMove) ObjectToMove.transform.position += Vector3.right * MoveOffset * (int)MoveDirection;
	}
}
