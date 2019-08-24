using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCAvatarEditor;
using UnityEditor;

public class SendData : ScriptableObject
{
    public EditorWindow window;
    public string filePath;
	public List<AnimationLoaderGUI.AnimParam> loadingProperties; 
}
