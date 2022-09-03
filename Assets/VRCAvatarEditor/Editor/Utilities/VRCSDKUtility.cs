using System.IO;
using System.Linq;
using UnityEditor;

namespace VRCAvatarEditor.Utilities
{
    public class VRCSDKUtility
    {
        /// <summary>
        /// VRCSDKに含まれるファイルを取得する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetVRCSDKFilePath(string fileName)
        {
            // VRCSDKフォルダが移動されている可能性があるため対象ファイルを探す
            return AssetDatabase.FindAssets(fileName)
                        .Select(g => AssetDatabase.GUIDToAssetPath(g))
                        .Where(p => p.Contains("/VRCSDK/") || p.Contains("VRChat Examples"))
                        .OrderBy(p => Path.GetFileName(p).Count())
                        .FirstOrDefault();
        }

        public static void UploadAvatar(bool newSDKUI)
        {
            EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
        }
    }
}