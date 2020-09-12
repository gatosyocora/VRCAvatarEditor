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

        public async void OnEnable()
        {
            FirstLoad();
        }

        public async void FirstLoad()
        {
            // UIがおかしくなるのを防止するために一度ローカルのデフォルトを読み込んでおく
            _ = LoadLanguage(BASE_LANGUAGE_PACK);

            await LoadLanguageTypesFromRemote();
            await LoadLanguage(EditorSetting.instance.Data.language, true);
        }

        public async Task LoadLanguage(string lang, bool fromRemote = false)
        {
            if (ExistLanguagePackInLocal(lang) && !fromRemote)
            {
                LoadLanguagePackFromLocal(lang);
            }
            else if (ExistLanguagePackInRemote(lang))
            {
                await LoadLanguagePackFromRemote(lang);
            }
            else
            {
                LoadLanguagePackFromLocal(BASE_LANGUAGE_PACK);
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

            // エラーやうまく取得できなかった場合
            // ローカルにあるBaseという名前の言語パックを使う
            if (string.IsNullOrEmpty(jsonData))
            {
                LoadLanguagePackFromLocal(BASE_LANGUAGE_PACK);
                return;
            }

            // 場合によってはローカルの言語パックを上書きしてしまうため新しくつくる
            // ScriptableObject内ではFromJsonOverwriteじゃないとうまくいかない
            langPair = CreateInstance<LanguageKeyPair>();
            langPair.name = lang;
            JsonUtility.FromJsonOverwrite(jsonData, langPair);
        }

        public void LoadLanguageTypesFromLocal(string editorFolderPath)
        {
            localLangs = Directory.GetFiles($"{editorFolderPath}{LANG_ASSET_FOLDER_PATH}", "*.asset")
                            .Select(f => Path.GetFileNameWithoutExtension(f))
                            .ToArray();
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

        private async Task LoadLanguageTypesFromRemote()
        {
            var jsonData = await LoadJsonDataFromGoogleSpreadSheetAsync("Types");

            // エラーやうまく取得できなかった場合はremoteLangsは空配列になる
            remoteLangs = Regex.Matches(jsonData, "\"[a-zA-Z]+\"?")
                            .Cast<Match>()
                            .Select(m => m.Value.Replace("\"", string.Empty))
                            .ToArray();

            if (localLangs != null)
            {
                langs = remoteLangs.Concat(localLangs).Distinct().ToArray();
            }
            else
            {
                langs = remoteLangs;
            }
            Debug.Log($"[VRCAvatarEditor] available language {string.Join(", ", langs)}");
        }


        private static async Task<string> LoadJsonDataFromGoogleSpreadSheetAsync(string sheetName)
        {
            var request = UnityWebRequest.Get($"{SPREAD_SHEET_API_URL}?sheetName={sheetName}");
            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                // エラーの場合は空文字を返す
                Debug.LogError(request.error);
                return string.Empty;
            }
            else
            {
                return request.downloadHandler.text;
            }
        }

        public bool ExistLanguagePackInLocal(string lang)
        {
            return localLangs != null && localLangs.Contains(lang);
        }

        private bool ExistLanguagePackInRemote(string lang)
        {
            return remoteLangs != null && remoteLangs.Contains(lang);
        }
    }
}
