﻿using System;
using System.Collections.Generic;
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

        private bool[] isTargets;

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
            currentShaderKindName = GetShaderKindName(edittingAvatar.materials);
            shaderKindNames = shaderKindGroups.Select(s => s.Key).ToArray();
            shaderKindIndex = Array.IndexOf(shaderKindNames, currentShaderKindName);

            // すべてtrueで初期化したnew bool[edittingAvatar.materials.Length]
            isTargets = Enumerable.Range(0, edittingAvatar.materials.Length).Select(b => true).ToArray();
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.shaderTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Duplicate selected"))
                    {
                        Undo.RegisterCompleteObjectUndo(originalAvatar.animator.gameObject, "Replace All Materials");
                        var srcMaterials = edittingAvatar.materials.Where((v, i) => isTargets[i]).ToArray();
                        var newMaterials = GatoUtility.DuplicateMaterials(srcMaterials);
                        for (int i = 0; i < newMaterials.Length; i++)
                        {
                            MaterialEdit.ReplaceMaterial(originalAvatar, srcMaterials[i], newMaterials[i]);
                            MaterialEdit.ReplaceMaterial(edittingAvatar, srcMaterials[i], newMaterials[i]);
                        }
                        Undo.SetCurrentGroupName("Replace All Materials");
                        Repaint();
                    }
                    if (GUILayout.Button("Optimize selected"))
                    {
                        foreach (var mat in edittingAvatar.materials.Where((v, i) => isTargets[i]).ToArray())
                        {
                            MaterialEdit.DeleteUnusedProperties(mat, AssetDatabase.GetAssetPath(mat));
                        }
                    }
                }

                EditorGUILayout.Space();

                using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScrollPosShader))
                {
                    leftScrollPosShader = scrollView.scrollPosition;
                    if (edittingAvatar.materials != null)
                    {
                        for (int i = 0; i < edittingAvatar.materials.Length; i++)
                        {
                            var mat = edittingAvatar.materials[i];
                            if (mat is null || mat.shader is null) continue;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    isTargets[i] = EditorGUILayout.ToggleLeft(string.Empty, isTargets[i], GUILayout.Width(30f));
                                    if (check.changed)
                                    {
                                        currentShaderKindName = GetShaderKindName(edittingAvatar.materials.Where((v, index) => isTargets[index]));
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
                                if (GUILayout.Button(LocalizeText.instance.langPair.edit))
                                {
                                    Selection.activeObject = mat;
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(currentShaderKindName);
                    GUILayout.Label("=>");
                    shaderKindIndex = EditorGUILayout.Popup(shaderKindIndex, shaderKindNames);

                    using (new EditorGUI.DisabledGroupScope(currentShaderKindName == shaderKindNames[shaderKindIndex]))
                    {
                        if (GUILayout.Button("Replace Shader"))
                        {
                            var materials = edittingAvatar.materials.Where((v, i) => isTargets[i]).ToArray();
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
        /// 一番多いShaderの種類を取得する
        /// </summary>
        /// <param name="materials"></param>
        /// <returns></returns>
        private string GetShaderKindName(IEnumerable<Material> materials)
            => materials
                .GroupBy(m => m.shader.name.Split('/').First())
                .OrderByDescending(x => x.Count())
                .First().Key;
    }
}
