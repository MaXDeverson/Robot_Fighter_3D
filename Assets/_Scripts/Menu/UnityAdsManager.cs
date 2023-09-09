//using UnityEngine;
//using System.Collections;
//using UnityEngine.Advertisements;

//public class UnityAdsManager : MonoBehaviour
//{
//	public static UnityAdsManager Instance;

//	[SerializeField]
//	private string androidGameID = "3899485", iOSGameID = "3899484";

//	[SerializeField]
//	private bool enableTestMode;

//	private void Awake()
//	{
//		if(Instance != null)
//		{
//			Destroy (this.gameObject);
//		}
//		else
//		{
//			Instance = this;
//			DontDestroyOnLoad (this.gameObject);
//		}
//	}

//	private void Start ()
//	{
//		string gameId = null;

//		#if UNITY_ANDROID
//		gameId = androidGameID;
//		#elif UNITY_IOS
//		gameId = iOSGameID;
//		#endif

//		if(Application.internetReachability != NetworkReachability.NotReachable)
//		{
//			if (string.IsNullOrEmpty(gameId))
//			{ 
//				// Make sure the Game ID is set.
//				Debug.LogError("Failed to initialize Unity Ads. Game ID is null or empty.");
//			}
//			else if (!Advertisement.isSupported)
//			{
//				Debug.LogWarning("Unable to initialize Unity Ads. Platform not supported.");
//			} 
//			else if (Advertisement.isInitialized)
//			{
//				Debug.Log("Unity Ads is already initialized.");
//			} 
//			else 
//			{
//				Debug.Log(string.Format("Initialize Unity Ads using Game ID {0} with Test Mode {1}.",
//					gameId, enableTestMode ? "enabled" : "disabled"));
//				Advertisement.Initialize(gameId, enableTestMode);
//			}
//		}
//	}

//	public bool IsVideoReady()
//	{
//		if(Application.internetReachability != NetworkReachability.NotReachable)
//		{
//			if(Advertisement.IsReady ())
//			{
//				return true;
//			}
//			else
//			{
//				return false;
//			}	
//		}
//		else
//		{
//			return false;
//		}
//	}

//	public void ShowUnityVideoAd()
//	{
//		if(Application.internetReachability != NetworkReachability.NotReachable)
//		{
//			if(Advertisement.IsReady())
//			{
//				Advertisement.Show ();
//			}	
//		}
//	}

//	public void ShowUnityRewardedVideoAd()
//	{
//		if(Application.internetReachability != NetworkReachability.NotReachable)
//		{
//			ShowOptions options = new ShowOptions();
//			options.resultCallback = HandleShowResult;

//			if(Advertisement.IsReady("rewardedVideo"))
//			{
//				Advertisement.Show ("rewardedVideo" , options);
//			}
//		}
//	}

//	private void HandleShowResult (ShowResult result)
//	{
//		switch (result)
//		{
//		case ShowResult.Finished:
//			Debug.Log ("Video completed.");
//			//if(MainMenuManager.Instance.freeCash)
//			//{
//			//	MainMenuManager.Instance.freeCash = false;
//			//	PlayerPrefs.SetInt ("CASH", PlayerPrefs.GetInt ("CASH") + 50);
//			//}
//			//if(MainMenuManager.Instance.freeGold)
//			//{
//			//	MainMenuManager.Instance.freeGold = false;
//			//	PlayerPrefs.SetInt ("GOLD", PlayerPrefs.GetInt ("GOLD") + 10);
//			//}
//			//if(MainMenuManager.Instance.freeEnergy)
//			//{
//			//	MainMenuManager.Instance.freeEnergy = false;
//			//	PlayerPrefs.SetInt ("ENERGY", PlayerPrefs.GetInt ("ENERGY") + 10);
//			//}

//			//MainMenuManager.Instance.UpdateText ();
//			break;
//		case ShowResult.Skipped:
//			Debug.LogWarning ("Video was skipped.");
//			break;
//		case ShowResult.Failed:
//			Debug.LogError ("Video failed to show.");
//			break;
//		}
//	}
//}