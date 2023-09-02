using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRCAvatarEditor;
using VRCAvatarEditor.Base;
using VRCAvatarEditor.Utilities;
using static VRCAvatarEditor.VRCAvatarEditorView;
using VRCAvatar = VRCAvatarEditor.Base.VRCAvatarBase;

namespace VRCAvatarEditor
{
    public class EditorSetting : ScriptableSingleton<EditorSetting>
    {
        private SettingData _data;
        public SettingData Data
        {
            get
            {
                if (_data is null) LoadSettingData();
                return _data;
            }
            private set => _data = value;
        }

        public void LoadSettingData()
        {
            var settingAsset = Resources.Load<SettingData>("CustomSettingData");

            if (settingAsset == null)
                settingAsset = Resources.Load<SettingData>("DefaultSettingData");

            _data = settingAsset;
        }

        /// <summary>
        /// 設定情報を読み込む
        /// </summary>
        public (LayoutType, string) LoadSettingDataFromScriptableObject(string editorFolderPath, string language,
                                                                        AvatarMonitorView avatarMonitorGUI, FaceEmotionViewBase faceEmotionGUI)
        {
            instance.LoadSettingData();

            LocalizeText.instance.LoadLanguageTypes(editorFolderPath);

            if (LocalizeText.instance.langPair == null)
            {
                LocalizeText.instance.FirstLoad();
            }

            if (string.IsNullOrEmpty(language) || instance.Data.language != LocalizeText.instance.langPair.name)
            {
                LocalizeText.instance.LoadLanguage(instance.Data.language);
            }

            var layoutType = instance.Data.layoutType;
            language = instance.Data.language;

            avatarMonitorGUI.LoadSettingData(instance.Data);
            faceEmotionGUI.LoadSettingData(instance.Data);

            return (layoutType, language);
        }

        /// <summary>
        /// 設定情報を保存する
        /// </summary>
        public void SaveSettingDataToScriptableObject(LayoutType layoutType, string language,
                                                        AvatarMonitorView avatarMonitorGUI, FaceEmotionViewBase faceEmotionGUI)
        {
            bool newCreated = false;
            var settingAsset = Resources.Load<SettingData>("CustomSettingData");

            if (settingAsset == null)
            {
                settingAsset = CreateInstance<SettingData>();
                newCreated = true;
            }

            avatarMonitorGUI.SaveSettingData(ref settingAsset);

            faceEmotionGUI.SaveSettingData(ref settingAsset);

            settingAsset.layoutType = layoutType;
            settingAsset.language = language;

            if (newCreated)
            {
                var data = Resources.Load<SettingData>("DefaultSettingData");
                var resourceFolderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(data)) + "/";
                AssetDatabase.CreateAsset(settingAsset, resourceFolderPath + "CustomSettingData.asset");
                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.SetDirty(settingAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 自分の設定情報を削除する
        /// </summary>
        public void DeleteMySettingData()
        {
            // 一度読み込んでみて存在するか確認
            var settingAsset = Resources.Load<SettingData>("CustomSettingData");
            if (settingAsset == null) return;

            AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(settingAsset.GetInstanceID()));
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 設定を反映する
        /// </summary>
        public void ApplySettingsToEditorGUI(VRCAvatar edittingAvatar, FaceEmotionViewBase faceEmotionGUI)
        {
            if (edittingAvatar.Animator == null) return;

            foreach (var skinnedMesh in edittingAvatar.SkinnedMeshList)
            {
                if (skinnedMesh.BlendShapeCount <= 0) continue;

                if (edittingAvatar.LipSyncShapeKeyNames != null && edittingAvatar.LipSyncShapeKeyNames.Count > 0)
                {
                    var exclusionsBlendShapes = VRCAvatarMeshUtility.GetExclusionBlendShapes(edittingAvatar, faceEmotionGUI.blendshapeExclusions);
                    skinnedMesh.SetExclusionBlendShapesByContains(exclusionsBlendShapes);
                }

                if (faceEmotionGUI.selectedSortType == FaceEmotionViewBase.SortType.AToZ)
                    skinnedMesh.SortBlendShapesToAscending();
                else
                    skinnedMesh.ResetDefaultSort();
            }
        }
    }
}
