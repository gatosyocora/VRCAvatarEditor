using System.IO;
using System.Linq;
using UnityEditor;

namespace VRCAvatarEditor.Utilities
{
    public class VRCSDKUtility
    {
        public const string ASSETS_FOLDER_PATH = "Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/";

        /// <summary>
        /// VRCSDKに含まれるファイルを取得する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetVRCSDKFilePath(string fileName)
        {
            return Path.Combine(ASSETS_FOLDER_PATH, fileName);
        }

        public static void UploadAvatar()
        {
            EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
        }
    }
}