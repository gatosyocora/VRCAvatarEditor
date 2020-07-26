using System;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class ShaderGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar edittingAvatar;
        private VRCAvatarEditor.Avatar originalAvatar;

        private Vector2 leftScrollPosShader = Vector2.zero;

        public void Initialize(ref VRCAvatarEditor.Avatar edittingAvatar, VRCAvatarEditor.Avatar originalAvatar)
        {
            this.edittingAvatar = edittingAvatar;
            this.originalAvatar = originalAvatar;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.shaderTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScrollPosShader))
                {
                    leftScrollPosShader = scrollView.scrollPosition;
                    if (edittingAvatar.materials != null)
                    {
                        foreach (var mat in edittingAvatar.materials)
                        {
                            if (mat == null) continue;
                            if (mat.shader == null) continue;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("" + mat.name + ".mat", GUILayout.Width(200f));
                                EditorGUILayout.LabelField(mat.shader.name);
                                if (GUILayout.Button("Duplicate"))
                                {
                                    var newMat = GatoUtility.DuplicateMaterial(mat);
                                    MaterialEdit.ReplaceMaterial(edittingAvatar, mat, newMat);
                                    MaterialEdit.ReplaceMaterial(originalAvatar, mat, newMat);
                                    Repaint();
                                }
                                if (GUILayout.Button(LocalizeText.instance.langPair.select))
                                {
                                    Selection.activeObject = mat;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }
    }
}
