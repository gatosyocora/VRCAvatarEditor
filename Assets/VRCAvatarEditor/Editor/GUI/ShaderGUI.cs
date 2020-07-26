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
        private IGrouping<string, Shader>[] shaderKindGroups;
        private string[] shaderKindNames;
        private string currentShaderKindName;
        private int shaderKindIndex = -1;

        private Vector2 leftScrollPosShader = Vector2.zero;

        public void Initialize(ref VRCAvatarEditor.Avatar edittingAvatar, VRCAvatarEditor.Avatar originalAvatar)
        {
            this.edittingAvatar = edittingAvatar;
            this.originalAvatar = originalAvatar;
            customShaders = GatoUtility.LoadShadersInProject();
            customShaderNames = customShaders.Select(s => s.name).ToArray();
            shaderKindGroups = customShaders
                                    .GroupBy(s => s.name.Split('/').First())
                                    .ToArray();
            currentShaderKindName = edittingAvatar.materials
                                        .GroupBy(m => m.shader.name.Split('/').First())
                                        .OrderByDescending(x => x.Count())
                                        .First().Key;
            shaderKindNames = shaderKindGroups.Select(s => s.Key).ToArray();
            shaderKindIndex = Array.IndexOf(shaderKindNames, currentShaderKindName);
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
                using (new EditorGUILayout.HorizontalScope())
                {
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
                        Undo.SetCurrentGroupName("Replace All Materials");
                    }
                    if (GUILayout.Button("All Optimize"))
                    {
                        foreach (var mat in edittingAvatar.materials)
                        {
                            MaterialEdit.DeleteUnusedProperties(mat, AssetDatabase.GetAssetPath(mat));
                        }
                    }
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(currentShaderKindName);
                    GUILayout.Label("=>");
                    shaderKindIndex = EditorGUILayout.Popup(shaderKindIndex, shaderKindNames);

                    if (GUILayout.Button("Replace Shader"))
                    {
                        var group = shaderKindGroups[shaderKindIndex];
                        if (group.Count() == 1)
                        {
                            var dstShader = group.Single();
                            foreach (var mat in edittingAvatar.materials)
                            {
                                mat.shader = dstShader;
                            }
                        }
                        else
                        {
                            var dstShaderGroup = shaderKindGroups[shaderKindIndex].Select(s => s).ToArray();
                            foreach (var mat in edittingAvatar.materials)
                            {
                                var srcShaderType = MaterialEdit.GetShaderType(mat);
                                var dstShaders = dstShaderGroup.Select((s, i) => new { Value = s, Index = i, Type = MaterialEdit.GetShaderType(s) });
                                int sameTypeCount = dstShaders.Where(s => s.Type == srcShaderType).Count();
                                int dstShaderIndex = -1;
                                // ShaderTypeが一致するShaderが1つだけあった
                                if (sameTypeCount == 1)
                                {
                                    dstShaderIndex = dstShaders.Where(s => s.Type == srcShaderType).Single().Index;
                                }
                                // ShaderTypeが一致するShaderが見つからなかった
                                else if (sameTypeCount == 0)
                                {
                                    // OpaqueのShaderにする（とりあえず一番最初）。
                                    // Opaqueがない場合とりあえずその種類で一番最初のShader
                                    dstShaderIndex = dstShaders
                                                        .Where(s => s.Type == MaterialEdit.ShaderType.Opaque)
                                                        .FirstOrDefault().Index;
                                }
                                // ShaderTypeが一致するShaderが複数見つかった
                                else
                                {
                                    // とりあえずShaderTypeが同じShaderの中の一番最初のShaderにする
                                    dstShaderIndex = dstShaders.Where(s => s.Type == srcShaderType).First().Index;
                                }
                                mat.shader = dstShaderGroup[dstShaderIndex];
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
