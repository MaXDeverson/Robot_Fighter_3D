using Firebase.Analytics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ATMBreak : MonoBehaviour,IDamagable<DamageObject>
{
	public string hitSFX = "";

	[Header("Gameobject Destroyed")]
	public GameObject destroyedGO;
	public bool orientToImpactDir;

	[Header("Spawn an item")]
	public GameObject spawnItem;
	public float spawnChance = 100;

	[Space(10)]
	public bool destroyOnHit;

	void Start()
	{
		gameObject.layer = LayerMask.NameToLayer("DestroyableObject");
	}

	//this object was Hit
	[SerializeField] private int _objectsCount;

	private bool _isSpawned;
	public void Hit(DamageObject DO)
	{
		//play hit sfx
		if (hitSFX != "")
		{
			GlobalAudioPlayer.PlaySFXAtPosition(hitSFX, transform.position);
		}
		float dir = Mathf.Sign(DO.inflictor.transform.position.x - transform.position.x);
		//spawn destroyed gameobject version
		if (destroyedGO != null)
		{
			GameObject BrokenGO = GameObject.Instantiate(destroyedGO);
			BrokenGO.transform.position = transform.position;
			
			//chance direction based on the impact direction
			if (orientToImpactDir && DO.inflictor != null)
			{
				
				Debug.Log("DIR:" + dir);
				BrokenGO.transform.rotation = Quaternion.LookRotation(Vector3.forward * dir);
			}
		}
		FirebaseAnalytics.LogEvent("atm_crush", "atm_crush", 0);
        if (!_isSpawned)
		{
			_isSpawned = true;
			for (int i = 1; i <= _objectsCount; i++)
			{
				GameObject item = GameObject.Instantiate(spawnItem, transform.position + new Vector3(Random.Range(-0.4f, 0.4f),0.2f * i, 0), Quaternion.identity);
				Vector3 forceDirection = new Vector3(Random.Range(2, 5) * -dir , Random.Range(1f, 3f), Random.Range(-1.5f, -2.5f) + (i* 0.5f));
				item.GetComponent<Rigidbody>().AddForce(forceDirection,ForceMode.Impulse);
			}
		}
		//destroy 
		if (destroyOnHit)
		{
			Destroy(gameObject);
		}
	}
}
