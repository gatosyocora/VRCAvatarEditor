using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRCAvatarEditor
{
    public interface IVRCAvatarEditorGUI
    {
        bool DrawGUI();
        void DrawSettingsGUI();
        void LoadSettingData(SettingData settingAsset);
        void SaveSettingData(ref SettingData settingAsset);
        void Dispose();
    }
}
