using Game.Privacy;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// The privacy buttons of the settings window: the policy page, always there, and the consent form again, only where
/// the law of the player's region requires it.
/// </summary>
/// <remarks>
/// Lives on the settings window (<c>Canvas/MainMenu/Settings</c> in the menu scene), so the options button is brought
/// up to date every time the window opens, and while it is open whenever the player's answer changes.
/// </remarks>
public class PrivacySettingsUI : MonoBehaviour
{
    [Inject] private IPrivacyService _privacy;

    [Tooltip("Opens the privacy policy page. Always there: Google Play requires the policy inside the game.")]
    [SerializeField] private Button _privacyPolicyButton;
    [Tooltip("Brings the consent form back. Shown only where the law of the player's region requires it.")]
    [SerializeField] private Button _privacyOptionsButton;

    private void Awake()
    {
        _privacyPolicyButton.onClick.AddListener(OpenPrivacyPolicy);
        _privacyOptionsButton.onClick.AddListener(ShowPrivacyOptions);
    }

    private void OnEnable()
    {
        _privacy.ConsentChanged += RefreshPrivacyOptions;
        RefreshPrivacyOptions();
    }

    private void OnDisable()
    {
        _privacy.ConsentChanged -= RefreshPrivacyOptions;
    }

    private void OpenPrivacyPolicy()
    {
        PlayClickSound();
        _privacy.OpenPrivacyPolicy();
    }

    private void ShowPrivacyOptions()
    {
        PlayClickSound();

        // The form is the consent platform's own and comes up over the game; the button waits until it has closed.
        _privacyOptionsButton.interactable = false;
        _privacy.ShowPrivacyOptions(() =>
        {
            if (this == null)
                return;

            _privacyOptionsButton.interactable = true;
            RefreshPrivacyOptions();
        });
    }

    private void RefreshPrivacyOptions()
    {
        _privacyOptionsButton.gameObject.SetActive(_privacy.IsPrivacyOptionsRequired);
    }

    private static void PlayClickSound()
    {
        // The menu's other buttons click through the same shared source.
        GameObject clickSound = GameObject.Find("ClickSound");
        if (clickSound != null)
            clickSound.GetComponent<AudioSource>().Play();
    }
}
