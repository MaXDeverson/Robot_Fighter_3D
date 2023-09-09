using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TrainingDummy : MonoBehaviour, IDamagable<DamageObject>{

	public string HitSound;
	public ParticleSystem[] Particles;
	private Animator animator;
	private Vector3 addForce;

	void Start(){
		animator = GetComponent<Animator>();
	}

	public void Hit(DamageObject DO){
		if(animator){

			//play animation
			animator.SetTrigger("Hit");

			//play sfx
			GlobalAudioPlayer.PlaySFXAtPosition(HitSound, transform.position);

			//set force for next fixed update
			int attackDir = (DO.inflictor.transform.position.x < transform.position.x)? -1 : 1; //the direction of the incoming attack
			addForce = Vector3.right * attackDir * -DO.knockBackForce; //add force in derection of the attack

			//play particles
			foreach(ParticleSystem particles in Particles){
				particles.Play();
			}
		}
	}

	//add force on physics update
	void FixedUpdate(){
		if(addForce.magnitude>0){
			GetComponent<Rigidbody>().velocity = addForce;
			addForce = Vector3.zero;
		}
	}
}
