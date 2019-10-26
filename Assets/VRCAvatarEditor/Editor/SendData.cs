using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCAvatarEditor;
using UnityEditor;

namespace VRCAvatarEditor
{
    public class SendData : ScriptableObject
    {
        public EditorWindow window;
        public string filePath;
        public List<FaceEmotion.AnimParam> loadingProperties;
    }
}