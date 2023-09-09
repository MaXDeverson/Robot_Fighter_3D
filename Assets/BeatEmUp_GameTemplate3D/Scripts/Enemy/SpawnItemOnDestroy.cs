using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItemOnDestroy : MonoBehaviour {

	public GameObject itemToSpawn;

	#if UNITY_EDITOR
	[HelpAttribute("The position offset where this object will spawn in relation to this character's position.", UnityEditor.MessageType.Info)]
	#endif

	[Space(15f)]

	public Vector3 SpawnOffset = new Vector3(0f, 1.5f, 0f);
	[Space(15f)]

	#if UNITY_EDITOR
	[HelpAttribute("The power at which this object is moved up before falling down", UnityEditor.MessageType.Info)]
	#endif

	public float spawnUpForce = 5f;

	public void Death(){
		if(!itemToSpawn) return;
		StartCoroutine(spawnItem());
	}

	IEnumerator spawnItem(){
		
		//spawn an item
		GameObject item = GameObject.Instantiate(itemToSpawn) as GameObject;
		item.transform.position = transform.position + SpawnOffset;

		yield return new WaitForFixedUpdate();

		//add an up force to this item
		Rigidbody rb = item.GetComponent<Rigidbody>();
		if(rb) rb.velocity = new Vector3(0, spawnUpForce, 0);
	}
}
