using System;
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

        public string[] toolTabTexts { get; private set; }
        public string[] animationTabTexts { get; private set; }

        public void OnEnable()
        {
            LoadLanguage("EN");
        }

        public async void LoadLanguage(string lang)
        {
            var jsonData = await LoadJsonDataFromGoogleSpreadSheetAsync(lang);
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
