using System.Collections.Generic;
using VRCAvatarEditor;
using UnityEditor;

namespace VRCAvatarEditor
{
    public class SendData : ScriptableSingleton<SendData>
    {
        public FaceEmotionGUI caller;
        public string filePath;
        public List<FaceEmotion.AnimParam> loadingProperties;
    }
}