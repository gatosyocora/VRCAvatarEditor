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

        public LanguageKeyPair langPair { get; private set; }

        public string[] langs { get; private set; }

        private string[] remoteLangs;
        private string[] localLangs;

        public string[] toolTabTexts { get; private set; }
        public string[] animationTabTexts { get; private set; }

        public void OnEnable()
        {
            LoadLanguageTypesFromRemote();
            LoadLanguage(EditorSetting.instance.Data.language);
        }

        public async void LoadLanguage(string lang)
        {
            if (localLangs.Contains(lang))
            {
                LoadLanguagePackFromLocal(lang);
            }
            else if (remoteLangs.Contains(lang))
            {
                await LoadLanguagePackFromRemote(lang);
            }

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

        private void LoadLanguagePackFromLocal(string lang)
        {
            langPair = Resources.Load<LanguageKeyPair>($"Lang/{lang}");
        }

        private async Task LoadLanguagePackFromRemote(string lang)
        {
            var jsonData = await LoadJsonDataFromGoogleSpreadSheetAsync(lang);
            langPair = CreateInstance<LanguageKeyPair>();
            JsonUtility.FromJsonOverwrite(jsonData, langPair);
        }

        public async void LoadLanguageTypesFromRemote()
        {
            var jsonText = await LoadJsonDataFromGoogleSpreadSheetAsync("Types");
            var matches = Regex.Matches(jsonText, "\"[a-zA-Z]+\"?");
            remoteLangs = matches.Cast<Match>().Select(m => m.Value).Select(v => v.Replace("\"", string.Empty)).ToArray();
            langs = remoteLangs.Concat(localLangs).Distinct().ToArray();
            Debug.Log($"[VRCAvatarEditor] available language {string.Join(", ", langs)}");
        }

        public void LoadLanguageTypesFromLocal(string editorFolderPath)
        {
            localLangs = Directory.GetFiles($"{editorFolderPath}{LANG_ASSET_FOLDER_PATH}", "*.asset")
                            .Select(f => Path.GetFileNameWithoutExtension(f))
                            .ToArray();
            langs = remoteLangs.Concat(localLangs).Distinct().ToArray();
            Debug.Log($"[VRCAvatarEditor] available language {string.Join(", ", langs)}");
        }

        public static async Task<string> LoadJsonDataFromGoogleSpreadSheetAsync(string sheetName)
        {
            var request = UnityWebRequest.Get($"{SPREAD_SHEET_API_URL}?sheetName={sheetName}");
            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                throw new Exception(request.error);
            }
            else
            {
                return request.downloadHandler.text;
            }
        }
    }
}
