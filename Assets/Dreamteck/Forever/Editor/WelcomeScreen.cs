namespace Dreamteck.Forever
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;

    [InitializeOnLoad]
    public class PluginInfo
    {
        public static string version = "1.17";
        private static bool open = false;
        static PluginInfo()
        {
            if (open) return;
            bool showInfo = EditorPrefs.GetString("Dreamteck.Forever.Info.version", "") != version;

            if (!showInfo)
            {
                var url = "https://dreamteck.io/plugins/forever/welcome.json";
                var prefKey = "Dreamteck.Forever.welcomeScreenVersion";
                var welcomeScreenVersion = EditorPrefs.GetInt(prefKey, -1);

                using (var mainDataReq = UnityWebRequest.Get(url))
                {
                    mainDataReq.SendWebRequest();

                    while (!mainDataReq.isDone || mainDataReq.result == UnityWebRequest.Result.InProgress)
                    {

                    }

                    if (mainDataReq.result == UnityWebRequest.Result.ProtocolError ||
                        mainDataReq.result == UnityWebRequest.Result.DataProcessingError ||
                        mainDataReq.result == UnityWebRequest.Result.ConnectionError)
                    {
                        Debug.LogError("An error occured while fetching the banners data.");
                    }
                    else
                    {
                        var jObj = JsonUtility.FromJson<WelcomeWindow.Data>(mainDataReq.downloadHandler.text);
                        welcomeScreenVersion = jObj.version;

                        var currentVersion = EditorPrefs.GetInt(prefKey, -1);

                        showInfo = currentVersion < welcomeScreenVersion;
                    }
                }
            }

            if (!showInfo) return;
            EditorPrefs.SetString("Dreamteck.Forever.Info.version", version);
            EditorApplication.update += OpenWindowOnUpdate;
        }

        private static void OpenWindowOnUpdate()
        {
            EditorApplication.update -= OpenWindowOnUpdate;
            EditorWindow.GetWindow<WelcomeScreen>(true);
            open = true;
        }
    }

    public class WelcomeScreen : WelcomeWindow
    {
        protected override Vector2 _windowSize => new Vector2(450, 640);

        [MenuItem("Window/Dreamteck/Forever/Start Screen")]
        public static void OpenWindow()
        {
            WelcomeScreen window = GetWindow<WelcomeScreen>(true);
            window.Load();
        }

        protected override void GetHeader()
        {
            header = ResourceUtility.EditorLoadTexture("Forever/Editor/Images", "forever_header");
        }

        public override void Load()
        {
            base.Load();
            SetTitle("Forever", "Forever");
            panels = new WindowPanel[6];
            panels[0] = new WindowPanel("Home", true, 0.25f);
            panels[1] = new WindowPanel("Changelog", false, panels[0], 0.25f);
            panels[2] = new WindowPanel("Learn", false, panels[0], 0.25f);
            panels[3] = new WindowPanel("Support", false, panels[0], 0.25f);
            panels[4] = new WindowPanel("Examples", false, panels[2], 0.25f);
            panels[5] = new WindowPanel("Playmaker", false, panels[0], 0.25f);



            panels[0].elements.Add(new WindowPanel.Space(400, 10));
            panels[0].elements.Add(new WindowPanel.Thumbnail("Utilities/Editor/Images", "changelog", "What's new?", "See all new features, important changes and bugfixes in " + PluginInfo.version, new ActionLink(panels[1], panels[0])));
            panels[0].elements.Add(new WindowPanel.Thumbnail("Utilities/Editor/Images", "get_started", "Get Started", "Learn how to use Forever in a matter of minutes.", new ActionLink(panels[2], panels[0])));
            panels[0].elements.Add(new WindowPanel.Thumbnail("Utilities/Editor/Images", "support", "Support", "Got a problem or a feature request? Our support is here to help!", new ActionLink(panels[3], panels[0])));

            _bannerData = LoadBannersData("https://dreamteck.io/plugins/forever/welcome.json", "Dreamteck.Forever.welcomeScreenVersion");

            if (_bannerData != null)
            {
                _textureWebRequests = new List<UnityWebRequest>();

                for (int i = 0; i < _bannerData.banners.Length; i++)
                {
                    var request = UnityWebRequestTexture.GetTexture(_bannerData.banners[i].bannerUrl);
                    request.SendWebRequest();
                    _textureWebRequests.Add(request);
                    _hasSentImageRequest = true;
                }

                if (_hasSentImageRequest)
                {
                    EditorApplication.update -= OnEditorUpdate;
                    EditorApplication.update += OnEditorUpdate;
                }
            }
            else
            {
                DrawFooter();
            }

            string path = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Forever/Editor");
            string changelogText = "";
            if (Directory.Exists(path))
            {
                if (File.Exists(path + "/changelog.txt"))
                {
                    string[] lines = File.ReadAllLines(path + "/changelog.txt");
                    changelogText = "";
                    for (int i = 0; i < lines.Length; i++)
                    {
                        changelogText += lines[i] + "\r\n";
                    }
                }
            }
            panels[1].elements.Add(new WindowPanel.Space(400, 10));
            panels[1].elements.Add(new WindowPanel.ScrollText(400, 400, changelogText));

            panels[2].elements.Add(new WindowPanel.Space(400, 10));
            panels[2].elements.Add(new WindowPanel.Thumbnail("Utilities/Editor/Images", "youtube", "Video Tutorials", "Watch a series of videos to get quikcly acquainted with Forever's workflow.", new ActionLink("https://www.youtube.com/c/Dreamteck")));
            panels[2].elements.Add(new WindowPanel.Thumbnail("Utilities/Editor/Images", "pdf", "User Manual", "Read a thorough documentation of the whole package along with a list of API methods.", new ActionLink("http://dreamteck.io/plugins/forever/user_manual.pdf")));
            panels[2].elements.Add(new WindowPanel.Thumbnail("Utilities/Editor/Images", "examples", "Examples", "Install examples in this project.", new ActionLink(panels[4], panels[2])));

            panels[2].elements.Add(new WindowPanel.Thumbnail("Utilities/Editor/Images", "rate", "Rate", "If you like Forever, please consider rating it on the Asset Store", new ActionLink("http://u3d.as/1t9T")));
            panels[2].elements.Add(new WindowPanel.Thumbnail("Forever/Editor/Images", "dreamteck_splines", "Splines", "Need a versatile and performant spline solution? Try Splines. It's integrated with Forever!", new ActionLink("http://u3d.as/sLk")));

            panels[3].elements.Add(new WindowPanel.Space(400, 10));
            panels[3].elements.Add(new WindowPanel.Thumbnail("Utilities/Editor/Images", "discord", "Discord Server", "Join our Discord community and chat with other developers and the team.", new ActionLink("https://discord.gg/bkYDq8v")));
            panels[3].elements.Add(new WindowPanel.Button(400, 30, "Contact Support", new ActionLink("http://dreamteck.io/team/contact.php?target=1")));



            panels[4].elements.Add(new WindowPanel.Space(400, 10));
            bool packagExists = false;
            string dir = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Forever/");
            if (Directory.Exists(dir))
            {
                if (File.Exists(dir + "/Examples.unitypackage")) packagExists = true;
            }
            if (packagExists) panels[4].elements.Add(new WindowPanel.Button(400, 30, "Install Examples", new ActionLink(InstallExamples)));
            else panels[4].elements.Add(new WindowPanel.Label("Examples package not found", null, Color.white));
            panels[5].elements.Add(new WindowPanel.Space(400, 10));
        }

        protected override void DrawFooter()
        {
            panels[0].elements.Add(new WindowPanel.Label("This window will not appear again automatically. To open it manually go to Window/Dreamteck/Forever/Start Screen", wrapText, new Color(1f, 1f, 1f, 0.5f), 400, 100));
        }

        private void InstallExamples()
        {
            string dir = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Forever/");
            AssetDatabase.ImportPackage(dir + "/Examples.unitypackage", false);
            EditorUtility.DisplayDialog("Import Complete", "Example scenes have been added to Dreamteck/Forever", "Yey!");
            panels[5].Back();
        }
    }
}
