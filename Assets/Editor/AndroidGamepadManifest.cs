using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEditor.Android;

/// <summary>
/// Declares in the Android manifest that the game supports a gamepad but does not need one:
/// <c>&lt;uses-feature android:name="android.hardware.gamepad" android:required="false" /&gt;</c>.
/// Google Play reads this declaration to mark the game as controller-compatible; required="false" keeps
/// the game available on phones without a controller.
/// Runs on every Android build, right after Unity generates the Gradle project. A gamepad feature that
/// is already declared (for example by Player Settings > Android Gamepad Support Level) is left as it is.
/// </summary>
public class AndroidGamepadManifest : IPostGenerateGradleAndroidProject
{
    private const string GamepadFeature = "android.hardware.gamepad";
    private static readonly XNamespace AndroidNs = "http://schemas.android.com/apk/res/android";

    public int callbackOrder => 0;

    // path is the unityLibrary module of the generated Gradle project
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        XDocument manifest = XDocument.Load(manifestPath);
        XElement root = manifest.Root;

        var features = root.Elements("uses-feature").ToList();
        if (features.Any(feature => (string)feature.Attribute(AndroidNs + "name") == GamepadFeature))
            return;

        var gamepad = new XElement("uses-feature",
            new XAttribute(AndroidNs + "name", GamepadFeature),
            new XAttribute(AndroidNs + "required", "false"));

        // <uses-feature> entries go before <application>, next to the ones Unity wrote
        if (features.Count > 0)
            features.Last().AddAfterSelf(gamepad);
        else
            root.AddFirst(gamepad);

        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using (XmlWriter writer = XmlWriter.Create(manifestPath, settings))
            manifest.Save(writer);
    }
}
