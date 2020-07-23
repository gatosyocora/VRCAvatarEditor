using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class LocalizeText : ScriptableSingleton<LocalizeText>
    {
        public LanguageKeyPair langPair { get; private set; }

        public string[] toolTabTexts { get; private set; }
        public string[] animationTabTexts { get; private set; }

        public void LoadLanguage(string lang)
        {
            langPair = Resources.Load<LanguageKeyPair>($"Lang/{lang}");
            toolTabTexts = new string[]
            {
                LocalizeText.instance.langPair.avatarInfoTitle,
                LocalizeText.instance.langPair.faceEmotionTitle,
                LocalizeText.instance.langPair.probeAnchorTitle,
                LocalizeText.instance.langPair.boundsTitle,
                LocalizeText.instance.langPair.shaderTitle
            };
            animationTabTexts = new string[]
            {
                langPair.standingTabText,
                langPair.sittingTabText
            };
        }
    }
}
