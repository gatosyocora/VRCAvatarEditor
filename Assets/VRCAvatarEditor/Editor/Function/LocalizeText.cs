using System;
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
        
        public LanguageKeyPair langPair { get; private set; }

        public string[] langs { get; private set; }

        public string[] toolTabTexts { get; private set; }
        public string[] animationTabTexts { get; private set; }

        public void OnEnable()
        {
            LoadLanguageTypes();
            LoadLanguage(EditorSetting.instance.Data.language);
        }

        public async void LoadLanguage(string lang)
        {
            var jsonData = await LoadJsonDataFromGoogleSpreadSheetAsync(lang);
            if (langPair is null) langPair = CreateInstance<LanguageKeyPair>();
            JsonUtility.FromJsonOverwrite(jsonData, langPair);
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

        public async void LoadLanguageTypes()
        {
            var jsonText = await LoadJsonDataFromGoogleSpreadSheetAsync("Types");
            var matches = Regex.Matches(jsonText, "\"[a-zA-Z]+\"?");
            langs = matches.Cast<Match>().Select(m => m.Value).Select(v => v.Replace("\"", string.Empty)).ToArray();
            Debug.Log($"[VRCAvatarEditor] usable language {string.Join(", ", langs)}");
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
