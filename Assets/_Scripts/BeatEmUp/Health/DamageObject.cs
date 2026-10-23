using UnityEngine;

[System.Serializable]
public class DamageObject {

	public string animTrigger = "";
	public int damage;
	public float duration = 1f;
	public float comboResetTime = .5f;
	public float forwardForce = 0f;
	public float knockBackForce = 0f;
	public string hitSFX = "";
	public bool knockDown;
	public bool slowMotionEffect;
	public bool DefenceOverride;
	[HideInInspector]
	public bool isGroundAttack;
	[HideInInspector]
	public AnimationClip animationClip;
	[HideInInspector]
	public float animationSpeed = 1f;

	[Header ("Hit Collider Settings")]
	public float CollSize;
	public float collDistance;
	public float collHeight;
	public float hitTime;

	[HideInInspector]
	public GameObject inflictor;

	public DamageObject(int _damage, GameObject _inflictor){
		damage =  _damage;
		inflictor = _inflictor;
	}
}