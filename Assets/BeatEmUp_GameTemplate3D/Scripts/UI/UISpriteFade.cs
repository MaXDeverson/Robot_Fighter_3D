using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//fades a sprite until it becomes invisible
[RequireComponent(typeof(Sprite))]
public class UISpriteFade : MonoBehaviour {

	private float Delay = .5f;
	private float fadeTime = 2f;
	private bool destroyAtFadeEnd = true;
	private Image image;

	IEnumerator Start(){
		yield return new WaitForSeconds(Delay);
		image = GetComponent<Image>();
			
		float t = 1;
		while(t > 0) {
			if(image) image.color = new Color(1f, 1f, 1f, t);
			t -= Time.deltaTime / fadeTime;
			yield return null;
		}

		image.color = new Color(1f, 1f, 1f, 0);
		if(destroyAtFadeEnd) Destroy(gameObject);
	}
}
