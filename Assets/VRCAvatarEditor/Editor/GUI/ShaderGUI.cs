using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Avatar = VRCAvatarEditor.VRCAvatar;

namespace VRCAvatarEditor
{
    public class ShaderGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatar edittingAvatar;
        private VRCAvatar originalAvatar;

        private Shader[] customShaders;
        private string[] customShaderNames;
        private IGrouping<string, Shader>[] shaderKindGroups;
        private string[] shaderKindNames;
        private string currentShaderKindName;
        private int shaderKindIndex = -1;

        private bool[] isTargets;
        private bool toggleAll = true;

        private Vector2 leftScrollPosShader = Vector2.zero;

        private readonly static string MULTIPLE = $"**<{nameof(MULTIPLE)}>**";
        private readonly static string NOSELECTION = $"--<{nameof(NOSELECTION)}>--";

        public void Initialize(VRCAvatar edittingAvatar, VRCAvatar originalAvatar)
        {
            this.edittingAvatar = edittingAvatar;
            this.originalAvatar = originalAvatar;
            customShaders = GatoUtility.LoadShadersInProject();
            customShaderNames = customShaders.Select(s => s.name).ToArray();
            shaderKindGroups = customShaders
                                    .GroupBy(s => s.name.Split('/').First())
                                    .ToArray();
            currentShaderKindName = GetShaderKindName(edittingAvatar.Materials);
            shaderKindNames = shaderKindGroups.Select(s => s.Key).ToArray();
            shaderKindIndex = Array.IndexOf(shaderKindNames, currentShaderKindName);

            // すべてtrueで初期化したnew bool[edittingAvatar.materials.Length]
            isTargets = Enumerable.Range(0, edittingAvatar.Materials.Length).Select(b => true).ToArray();
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.shaderTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        toggleAll = EditorGUILayout.ToggleLeft(LocalizeText.instance.langPair.toggleAll, toggleAll);
                        if (check.changed)
                        {
                            isTargets = Enumerable.Range(0, edittingAvatar.Materials.Length).Select(b => toggleAll).ToArray();
                            currentShaderKindName = GetShaderKindName(edittingAvatar.Materials.Where((v, index) => isTargets[index]));
                            shaderKindIndex = Array.IndexOf(shaderKindNames, currentShaderKindName);
                            Repaint();
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(LocalizeText.instance.langPair.duplicateSelectedButtonText))
                    {
                        Undo.RegisterCompleteObjectUndo(originalAvatar.Animator.gameObject, "Replace All Materials");
                        var srcMaterials = edittingAvatar.Materials.Where((v, i) => isTargets[i]).ToArray();
                        var newMaterials = GatoUtility.DuplicateMaterials(srcMaterials);
                        for (int i = 0; i < newMaterials.Length; i++)
                        {
                            MaterialEdit.ReplaceMaterial(originalAvatar, srcMaterials[i], newMaterials[i]);
                            MaterialEdit.ReplaceMaterial(edittingAvatar, srcMaterials[i], newMaterials[i]);
                        }
                        Undo.SetCurrentGroupName("Replace All Materials");
                        Repaint();
                    }
                    if (GUILayout.Button(LocalizeText.instance.langPair.optimizeSelectedButtonText))
                    {
                        foreach (var mat in edittingAvatar.Materials.Where((v, i) => isTargets[i]).ToArray())
                        {
                            MaterialEdit.DeleteUnusedProperties(mat, AssetDatabase.GetAssetPath(mat));
                        }
                    }
                }

                EditorGUILayout.Space();

                using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScrollPosShader))
                {
                    leftScrollPosShader = scrollView.scrollPosition;
                    if (edittingAvatar.Materials != null)
                    {
                        for (int i = 0; i < edittingAvatar.Materials.Length; i++)
                        {
                            var mat = edittingAvatar.Materials[i];
                            if (mat is null || mat.shader is null) continue;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    isTargets[i] = EditorGUILayout.ToggleLeft(string.Empty, isTargets[i], GUILayout.Width(30f));
                                    if (check.changed)
                                    {
                                        currentShaderKindName = GetShaderKindName(edittingAvatar.Materials.Where((v, index) => isTargets[index]));
                                        shaderKindIndex = Array.IndexOf(shaderKindNames, currentShaderKindName);
                                        Repaint();
                                    }
                                }

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
                                        currentShaderKindName = GetShaderKindName(edittingAvatar.Materials.Where((v, index) => isTargets[index]));
                                        shaderKindIndex = Array.IndexOf(shaderKindNames, currentShaderKindName);
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
                                        currentShaderKindName = GetShaderKindName(edittingAvatar.Materials.Where((v, index) => isTargets[index]));
                                        shaderKindIndex = Array.IndexOf(shaderKindNames, currentShaderKindName);
                                        Repaint();
                                    }
                                }
                                if (GUILayout.Button(LocalizeText.instance.langPair.edit))
                                {
                                    Selection.activeObject = mat;
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.HelpBox(LocalizeText.instance.langPair.useInspectorMessageText, MessageType.Info);

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(LocalizeIfNeeded(currentShaderKindName));
                    GUILayout.Label("=>");
                    shaderKindIndex = EditorGUILayout.Popup(shaderKindIndex, shaderKindNames);

                    using (new EditorGUI.DisabledGroupScope(shaderKindIndex == -1 || currentShaderKindName == NOSELECTION || currentShaderKindName == shaderKindNames[shaderKindIndex]))
                    {
                        if (GUILayout.Button(LocalizeText.instance.langPair.replaceShaderButtonText))
                        {
                            var materials = edittingAvatar.Materials.Where((v, i) => isTargets[i]).ToArray();
                            var group = shaderKindGroups[shaderKindIndex];
                            if (group.Count() == 1)
                            {
                                var dstShader = group.Single();
                                foreach (var mat in materials)
                                {
                                    mat.shader = dstShader;
                                }
                            }
                            else
                            {
                                var dstShaderGroup = shaderKindGroups[shaderKindIndex].Select(s => s).ToArray();
                                foreach (var mat in materials)
                                {
                                    var dstShader = MaterialEdit.CalculateSimilarShader(dstShaderGroup, mat.shader);
                                    mat.shader = dstShader;
                                }
                            }

                            currentShaderKindName = shaderKindNames[shaderKindIndex];

                            Repaint();
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

        /// <summary>
        /// 共通するShaderの種類を取得する。異なるものが含まれている場合はそれを示す文字列を返す
        /// </summary>
        /// <param name="materials"></param>
        /// <returns></returns>
        private string GetShaderKindName(IEnumerable<Material> materials)
        {
            var shaderKinds = materials.GroupBy(m => m.shader.name.Split('/').First());

            var count = shaderKinds.Count();
            if (count == 1)
            {
                return shaderKinds.Single().Key;
            }
            else if (count == 0)
            {
                return NOSELECTION;
            }
            else
            {
                return MULTIPLE;
            }
        }

        private string LocalizeIfNeeded(string shaderKindName)
        {
            if (shaderKindName.Contains($"<{nameof(NOSELECTION)}>"))
            {
                return shaderKindName.Replace($"<{nameof(NOSELECTION)}>", LocalizeText.instance.langPair.noSelectionText);
            }
            else if (shaderKindName.Contains($"<{nameof(MULTIPLE)}>"))
            {
                return shaderKindName.Replace($"<{nameof(MULTIPLE)}>", LocalizeText.instance.langPair.multipleText);
            }
            else
            {
                return shaderKindName;
            }
        }
    }
}
