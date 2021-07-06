using Amazon.Auth.AccessControlPolicy;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace VRCAvatarEditor
{
    public class LocalizeText : ScriptableSingleton<LocalizeText>
    {
        private readonly static string SPREAD_SHEET_API_URL = "https://script.google.com/macros/s/AKfycbw-TO0isxWGraxcYj66BTG0KHfWqvf1NScNh7gOd7Ku6cHfDavo/exec";
        private readonly static string LANG_ASSET_FOLDER_PATH = "Editor/Resources/Lang";
        private readonly static string BASE_LANGUAGE_PACK = "Base";

        public LanguageKeyPair langPair { get; private set; }

        public string[] langs { get; private set; }

        private string[] remoteLangs;
        private string[] localLangs;

        public string[] toolTabTexts { get; private set; }
        public string[] animationTabTexts { get; private set; }

        public void OnEnable()
        {
            FirstLoad();
        }

        public void FirstLoad()
        {
            LoadLanguage(BASE_LANGUAGE_PACK);
        }

        public void LoadLanguage(string lang)
        {
            if (!ExistLanguagePackInLocal(lang)) return;

            langPair = Resources.Load<LanguageKeyPair>($"Lang/{lang}");

            Debug.Log($"[VRCAvatarEditor] Loaded LanguagePack {lang}.");

            toolTabTexts = new string[]
            {
                langPair.avatarInfoTitle,
                langPair.faceEmotionTitle,
                langPair.probeAnchorTitle,
                langPair.boundsTitle,
                langPair.shaderTitle
            };
            animationTabTexts = new string[]
            {
                langPair.standingTabText,
                langPair.sittingTabText
            };
        }

        public void LoadLanguageTypesFromLocal(string editorFolderPath)
        {
            localLangs = GetLanguageTypes(editorFolderPath);
            if (remoteLangs != null)
            {
                langs = remoteLangs.Concat(localLangs).Distinct().ToArray();
            }
            else
            {
                langs = localLangs;
            }
            Debug.Log($"[VRCAvatarEditor] available language {string.Join(", ", langs)}");
        }

        private string[] GetLanguageTypes(string editorFolderPath)
            => Directory.GetFiles($"{editorFolderPath}{LANG_ASSET_FOLDER_PATH}", "*.asset")
                            .Select(f => Path.GetFileNameWithoutExtension(f))
                            .ToArray();

        public bool ExistLanguagePackInLocal(string lang)
        {
            return localLangs != null && localLangs.Contains(lang);
        }
    }
}
