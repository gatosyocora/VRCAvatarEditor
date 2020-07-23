using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class LocalizeText : ScriptableSingleton<LocalizeText>
    {
        public LanguageKeyPair langPair { get; private set; }

        public void LoadLanguage(string lang)
        {
            langPair = Resources.Load<LanguageKeyPair>($"Lang/{lang}");
        }
    }
}
