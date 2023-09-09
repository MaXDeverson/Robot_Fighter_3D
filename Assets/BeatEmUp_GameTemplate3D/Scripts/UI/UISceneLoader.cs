using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UISceneLoader : MonoBehaviour {

	private bool loadSceneInProgress;

	//load a new scene
	public void LoadScene(string sceneName){
		if(!loadSceneInProgress) StartCoroutine(LoadSceneCoroutine(sceneName));
	}

	IEnumerator LoadSceneCoroutine(string sceneName){
		loadSceneInProgress = true;

		//Fade out screen
		UIFader fader = GameObject.FindObjectOfType<UIFader>();
		if(fader != null) fader.Fade(UIFader.FADE.FadeOut, 0.4f, 0.4f);
		yield return new WaitForSeconds(1f);

		//Load new scene
		SceneManager.LoadScene(sceneName);

		loadSceneInProgress = false;
	}
}
