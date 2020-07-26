using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class ShaderGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar edittingAvatar;
        private VRCAvatarEditor.Avatar originalAvatar;

        private Shader[] customShaders;
        private string[] customShaderNames;

        private Vector2 leftScrollPosShader = Vector2.zero;

        public void Initialize(ref VRCAvatarEditor.Avatar edittingAvatar, VRCAvatarEditor.Avatar originalAvatar)
        {
            this.edittingAvatar = edittingAvatar;
            this.originalAvatar = originalAvatar;
            customShaders = GatoUtility.LoadShadersInProject();
            customShaderNames = customShaders.Select(s => s.name).ToArray();
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
                            if (mat is null || mat.shader is null) continue;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    var material = EditorGUILayout.ObjectField(
                                                        string.Empty,
                                                        mat,
                                                        typeof(Material),
                                                        true,
                                                        GUILayout.Width(200f)) as Material;
                                    if (check.changed && material != null)
                                    {
                                        MaterialEdit.ReplaceMaterial(edittingAvatar, mat, material);
                                        MaterialEdit.ReplaceMaterial(originalAvatar, mat, material);
                                        Repaint();
                                    }
                                }

                                int shaderIndex = Array.IndexOf(customShaders, mat.shader);
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    shaderIndex = EditorGUILayout.Popup(shaderIndex, customShaderNames);
                                    if (check.changed)
                                    {
                                        mat.shader = customShaders[shaderIndex];
                                    }
                                }
                                if (GUILayout.Button("Duplicate"))
                                {
                                    var newMat = GatoUtility.DuplicateMaterial(mat);
                                    MaterialEdit.ReplaceMaterial(edittingAvatar, mat, newMat);
                                    MaterialEdit.ReplaceMaterial(originalAvatar, mat, newMat);
                                    Repaint();
                                }
                                if (GUILayout.Button(LocalizeText.instance.langPair.edit))
                                {
                                    Selection.activeObject = mat;
                                }
                            }
                        }
                    }
                }
                if (GUILayout.Button("All Duplicate"))
                {
                    Undo.RegisterCompleteObjectUndo(originalAvatar.animator.gameObject, "Replace All Materials");
                    var srcMaterials = edittingAvatar.materials;
                    var newMaterials = GatoUtility.DuplicateMaterials(srcMaterials);
                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        MaterialEdit.ReplaceMaterial(originalAvatar, srcMaterials[i], newMaterials[i]);
                        MaterialEdit.ReplaceMaterial(edittingAvatar, srcMaterials[i], newMaterials[i]);
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
