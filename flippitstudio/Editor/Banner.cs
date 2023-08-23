using UnityEditor;
using UnityEngine;

namespace Flippit.Editor
{
    public class Banner
    {
        private const string BANNER_PATH = "Assets/Plugins/Flippit/Editor/Banniere.png";

        private readonly Texture2D banner;
        private readonly GUIStyle versionTextStyle;

        private const int BANNER_WIDTH = 460;
        private const int BANNER_HEIGHT = 205;

        private const int FONT_SIZE = 14;

        public Banner()
        {
            banner = AssetDatabase.LoadAssetAtPath<Texture2D>(BANNER_PATH);
            versionTextStyle = new GUIStyle
            {
                fontSize = FONT_SIZE,
                richText = true,
                fontStyle = FontStyle.Bold
            };
            versionTextStyle.normal.textColor = Color.white;
            versionTextStyle.alignment = TextAnchor.UpperRight;
        }

        public void DrawBanner(Rect position)
        {
            var rect = new Rect((position.size.x - BANNER_WIDTH) / 2, 0, BANNER_WIDTH, BANNER_HEIGHT);
            GUI.DrawTexture(rect, banner);

            //var versionText = new Rect((position.width + BANNER_WIDTH) / 2 - 10, 10, 0, 0);
            //EditorGUI.DropShadowLabel(versionText, ApplicationData.GetData().SDKVersion, versionTextStyle);

            GUILayout.Space(BANNER_HEIGHT);
        }
    }
}
