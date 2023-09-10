using UnityEngine;

public class WeaponPickup : PickUpable{

	[Header("Weapon Settings")]
	public Weapon weapon;

    private void Awake()
    {
		type = PickUpType.Weapon;
    }
    //pick up this item
    public override void OnPickup(GameObject player){
		GiveWeaponToPlayer(player);
		base.OnPickup(player);
	}

	public void GiveWeaponToPlayer(GameObject player){
		Debug.LogError("GIVE WEAPON");
		PlayerCombat pc = player.GetComponent<PlayerCombat>();
		if(pc) pc.equipWeapon(weapon);
	}
}

public abstract class PickUpable : MonoBehaviour
{
	[Header("Pickup Settings")]
	public string SFX = "";
	public GameObject pickupEffect;
	public float pickUpRange = 1;
	public PickUpType type;
	protected GameObject[] Players;
	protected GameObject playerinRange;

	void Start()
	{
		Players = GameObject.FindGameObjectsWithTag("Player");
	}

	//Checks if this item is in pickup range
	void LateUpdate()
	{
		foreach (GameObject player in Players)
		{
			if (player)
			{
				float distanceToPlayer = Vector3.Distance(player.transform.position, transform.position);

				//item in pickup range
				if (distanceToPlayer < pickUpRange && playerinRange == null)
				{
					playerinRange = player;
					player.SendMessage("ItemInRange", gameObject, SendMessageOptions.DontRequireReceiver);
					return;

				}

				//item out of pickup range
				if (distanceToPlayer > pickUpRange && playerinRange != null)
				{
					player.SendMessage("ItemOutOfRange", gameObject, SendMessageOptions.DontRequireReceiver);
					playerinRange = null;
				}
			}
		}
	}

	//pick up this item
	public virtual void OnPickup(GameObject player)
	{

		//show pickup effect
		if (pickupEffect)
		{
			GameObject effect = GameObject.Instantiate(pickupEffect);
			effect.transform.position = transform.position;
		}

		//play sfx
		if (SFX != null) GlobalAudioPlayer.PlaySFX(SFX);

		//give weapon to player
		//GiveWeaponToPlayer(player);

		//remove pickup
		Destroy(gameObject);
	}
#if UNITY_EDITOR

	//Show pickup range
	void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, pickUpRange);
	}
#endif
}



public enum PickUpType
{
	Weapon,
	Money,
}