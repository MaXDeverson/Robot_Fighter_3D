using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;
// Placing the Purchaser class in the CompleteProject namespace allows it to interact with ScoreManager,
// one of the existing Survival Shooter scripts.
namespace CompleteProject
{
    // Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity Purchasing.
    public class Purchaser : MonoBehaviour, IStoreListener
    {
        private static IStoreController m_StoreController;
        ////The Unity Purchasing system.
        private static IExtensionProvider m_StoreExtensionProvider;
        //The store-specific Purchasing subsystems.

        public static string remove_ads_ninja = "remove_ads_ninja";
        public static string unlock_levels_ninja = "unlock_levels_ninja";
        public static string unlock_players_ninja = "unlock_players_ninja";
        public static string unlock_everything_ninja = "unlock_everything_ninja";
        public static string coins5k = "5k_coins";
        public static string coins15k = "15k_coins";
        public static string coins40k = "40k_coins";
        public static string coins60k = "60k_coins";

        void Awake()
        {
            //ConsoliAds.Instance.initialize(true);
        }

        void Start()
        {
            //If we haven't set up the Unity Purchasing reference
            if (m_StoreController == null)
            {
                // Begin to configure our connection to Purchasing
                 InitializePurchasing();
            }
        }

        public void InitializePurchasing()
        {
            // If we have already connected to Purchasing...
            if (IsInitialized())
            {
                // ... we are done here.
                return;
            }

            //Create a builder, first passing in a suite of Unity provided stores.
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(remove_ads_ninja, ProductType.NonConsumable);
            builder.AddProduct(unlock_levels_ninja, ProductType.NonConsumable);
            builder.AddProduct(unlock_players_ninja, ProductType.NonConsumable);
            builder.AddProduct(unlock_everything_ninja, ProductType.NonConsumable);

            builder.AddProduct(coins5k, ProductType.Consumable);
            builder.AddProduct(coins15k, ProductType.Consumable);
            builder.AddProduct(coins40k, ProductType.Consumable);
            builder.AddProduct(coins60k, ProductType.Consumable);



            //Kick off the remainder of the set - up with an asynchrounous call, passing the configuration
            // and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
            UnityPurchasing.Initialize(this, builder);
        }


        private bool IsInitialized()
        {
            // Only say we are initialized if both the Purchasing references are set.
            //return m_StoreController != null && m_StoreExtensionProvider != null;
            return true;
        }


        public void Buy_remove_ads_ninja()
        {
            BuyProductID(remove_ads_ninja);
        }
        public void Levels_unlock()
        {
            BuyProductID(unlock_levels_ninja);
        }
        public void Robots_unlock()
        {
            BuyProductID(unlock_players_ninja);
        }
        public void Everything_unlock()
        {
            BuyProductID(unlock_everything_ninja);
        }

        public void Buy_5k()
        {
            BuyProductID(coins5k);
        }
        public void Buy_15k()
        {
            BuyProductID(coins15k);
        }
        public void Buy_40k()
        {
            BuyProductID(coins40k);
        }
        public void Buy_60k()
        {
            BuyProductID(coins60k);
        }

        void BuyProductID(string productId)
        {
            // If Purchasing has been initialized...
            if (IsInitialized())
            {
                //  ... look up the Product reference with the general product identifier and the Purchasing
                // system's products collection.
                Product product = m_StoreController.products.WithID(productId);

                // If the look up found a product for this device's store and that product is ready to be sold ... 
                if (product != null && product.availableToPurchase)
                {
                    Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                    //   ... buy the product.Expect a response either through ProcessPurchase or OnPurchaseFailed
                    // asynchronously.
                    m_StoreController.InitiatePurchase(product);
                }
                // Otherwise...
                else
                {
                    // ... report the product look-up failure situation
                    Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                }
            }
            // Otherwise...
            else
            {
                // ... report the fact Purchasing has not succeeded initializing yet.Consider waiting longer or
                //  retrying initiailization.
                Debug.Log("BuyProductID FAIL. Not initialized.");
            }
        }


       // Restore purchases previously made by this customer.Some platforms automatically restore purchases, like Google.

       // Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
        public void RestorePurchases()
        {
           // If Purchasing has not yet been set up ...
            if (!IsInitialized())
            {
               // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
                Debug.Log("RestorePurchases FAIL. Not initialized.");
                return;
            }

           // If we are running on an Apple device ... 
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
               // ... begin restoring purchases
                Debug.Log("RestorePurchases started ...");

              //  Fetch the Apple store-specific subsystem.
              // var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
               // Begin the asynchronous process of restoring purchases.Expect a confirmation response in 
                 //the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
                //apple.RestoreTransactions((result) =>
                //{
                //  //  The first phase of restoration.If no more responses are received on ProcessPurchase then
                //  // no purchases are available to be restored.
                //    Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
                //});
            }
           // Otherwise...
            else
            {
               // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
            }
        }



        //--- IStoreListener
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Purchasing has succeeded initializing. Collect our Purchasing references.
            Debug.Log("OnInitialized: PASS");

            //  Overall Purchasing system, configured with products for this application.
            m_StoreController = controller;
            // Store specific subsystem, for accessing device-specific store features.
            m_StoreExtensionProvider = extensions;
        }


        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            //Purchasing set-up has not succeeded.Check error for reason.Consider sharing this reason with the user.
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
        }
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            //Purchasing set-up has not succeeded.Check error for reason.Consider sharing this reason with the user.
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            // A consumable product has been purchased by this user.

            if (String.Equals(args.purchasedProduct.definition.id, remove_ads_ninja, StringComparison.Ordinal))
            {
                Debug.Log(string.Format("yahoooooooooo ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

                //  Paste Here
                PlayerPrefs.SetInt("ADSUNLOCK", 1);
                //if (McFairyAdsMediation.Instance)
                //    McFairyAdsMediation.Instance.HideBanner();

            }
            else if (String.Equals(args.purchasedProduct.definition.id, unlock_players_ninja, StringComparison.Ordinal))
            {
                Debug.Log(string.Format("yahoooooooooo ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

                // Paste Here
                PlayerPrefs.SetInt("jetPrice_01", 0); PlayerPrefs.SetInt("jetPrice_02", 0);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            }
            else if (String.Equals(args.purchasedProduct.definition.id, unlock_levels_ninja, StringComparison.Ordinal))
            {
                Debug.Log(string.Format("yahoooooooooo ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

                // Paste Here
                PlayerPrefs.SetInt("UnlockedLevels", 19);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            }
            else if (String.Equals(args.purchasedProduct.definition.id, unlock_everything_ninja, StringComparison.Ordinal))
            {
                Debug.Log(string.Format("yahoooooooooo ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

                //Paste Here
                PlayerPrefs.SetInt("ADSUNLOCK", 1);
                //if (McFairyAdsMediation.Instance)
                //    McFairyAdsMediation.Instance.HideBanner();
                PlayerPrefs.SetInt("jetPrice_01", 0); PlayerPrefs.SetInt("jetPrice_02", 0);
                PlayerPrefs.SetInt("UnlockedLevels", 19);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else if (String.Equals(args.purchasedProduct.definition.id, coins5k, StringComparison.Ordinal))
            {
                Debug.Log(string.Format("yahoooooooooo ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

                //Paste Here
                PlayerPrefs.SetInt("TotalScore", PlayerPrefs.GetInt("TotalScore") + 5000);

            }
            else if (String.Equals(args.purchasedProduct.definition.id, coins15k, StringComparison.Ordinal))
            {
                Debug.Log(string.Format("yahoooooooooo ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

                // Paste Here
                PlayerPrefs.SetInt("TotalScore", PlayerPrefs.GetInt("TotalScore") + 15000);

            }
            else if (String.Equals(args.purchasedProduct.definition.id, coins40k, StringComparison.Ordinal))
            {
                Debug.Log(string.Format("yahoooooooooo ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

                //Paste Here
                PlayerPrefs.SetInt("TotalScore", PlayerPrefs.GetInt("TotalScore") + 40000);

            }
            else if (String.Equals(args.purchasedProduct.definition.id, coins60k, StringComparison.Ordinal))
            {
                Debug.Log(string.Format("yahoooooooooo ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

                // Paste Here
                PlayerPrefs.SetInt("TotalScore", PlayerPrefs.GetInt("TotalScore") + 60000);

            }
            return PurchaseProcessingResult.Complete;
            Application.LoadLevel(Application.loadedLevel);
        }


        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            // A product purchase attempt did not succeed.Check failureReason for more detail. Consider sharing

            //this reason with the user to guide their troubleshooting actions.
            Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        }
    }
}