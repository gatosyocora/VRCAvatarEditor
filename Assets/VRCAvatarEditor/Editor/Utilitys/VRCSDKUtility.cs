using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor.Utilitys
{
    public class VRCSDKUtility
    {
        /// <summary>
        /// VRCSDKのバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public static string GetVRCSDKVersion()
        {
            string path = GetVRCSDKFilePath("version");
            return FileUtility.GetFileTexts(path);
        }

        /// <summary>
        /// VRCSDKに含まれるファイルを取得する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetVRCSDKFilePath(string fileName)
        {
            // VRCSDKフォルダが移動されている可能性があるため対象ファイルを探す
            var guids = AssetDatabase.FindAssets(fileName, null);
            string path = string.Empty;
            bool couldFindFile = false;
            foreach (var guid in guids)
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("VRCSDK/"))
                {
                    couldFindFile = true;
                    break;
                }
            }
            if (couldFindFile)
                return path;
            else
                return string.Empty;
        }

        /// <summary>
        /// VRCSDKが新しいUIかどうか
        /// </summary>
        /// <returns></returns>
        public static bool IsNewSDKUI()
        {
            var sdkVersion = GetVRCSDKVersion();
            // 新UI以降のバージョンにはファイルが存在するため何かしらは返ってくる
            if (string.IsNullOrEmpty(sdkVersion)) return false;

            var dotChar = '.';
            var zero = '0';
            var versions = sdkVersion.Split(dotChar);
            var version =
                    versions[0].PadLeft(4, zero) + dotChar +
                    versions[1].PadLeft(2, zero) + dotChar +
                    versions[2].PadLeft(2, zero);
            var newVersion = "2019.08.23";

            return newVersion.CompareTo(version) <= 0;
        }
    }
}