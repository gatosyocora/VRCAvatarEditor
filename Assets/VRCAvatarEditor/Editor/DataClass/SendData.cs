using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCAvatarEditor;
using UnityEditor;

namespace VRCAvatarEditor
{
    public class SendData : ScriptableSingleton<SendData>
    {
        public EditorWindow window;
        public FaceEmotionGUI caller;
        public string filePath;
        public List<FaceEmotion.AnimParam> loadingProperties;
    }
}