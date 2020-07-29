using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRCAvatarEditor;

namespace VRCAvatarEditor
{
    public class EditorSetting : ScriptableSingleton<EditorSetting>
    {
        private SettingData _data;
        public SettingData Data
        {
            get
            {
                if (_data is null) _data = LoadSettingData();
                return _data;
            }
            private set => _data = value;
        }

        private static SettingData LoadSettingData()
        {
            var settingAsset = Resources.Load<SettingData>("CustomSettingData");

            if (settingAsset == null)
                settingAsset = Resources.Load<SettingData>("DefaultSettingData");

            return settingAsset;
        }
    }
}
