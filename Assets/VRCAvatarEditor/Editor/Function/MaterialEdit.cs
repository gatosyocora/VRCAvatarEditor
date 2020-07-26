using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class MaterialEdit
    {
        public enum ShaderType
        {
            Opaque,
            Transparent,
            TransparentCutout,
            Background,
            Overlay
        };

        /// <summary>
        /// Materialを置き換える
        /// </summary>
        /// <param name="avatar">Materialを置き換えるアバター</param>
        /// <param name="srcMaterial">変更前のMaterial</param>
        /// <param name="dstMaterial">変更後のMaterial</param>
        public static void ReplaceMaterial(VRCAvatarEditor.Avatar avatar, Material srcMaterial, Material dstMaterial)
        {
            GatoUtility.ReplaceMaterial(avatar.animator.gameObject, srcMaterial, dstMaterial);
            var index = Array.IndexOf(avatar.materials, srcMaterial);
            if (index == -1) return;
            avatar.materials[index] = dstMaterial;
        }

        /// <summary>
        /// MaterialからShaderの種類を取得する
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static ShaderType GetShaderType(Material material)
        {
            var shaderType = material.GetTag("RenderType", false);
            Enum.TryParse(shaderType, out ShaderType type);
            return type;
        }

        /// <summary>
        /// ShaderからShaderの種類を取得する
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public static ShaderType GetShaderType(Shader shader)
        {
            return GetShaderType(new Material(shader));
        }
    }
}
