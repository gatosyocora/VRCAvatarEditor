using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class MaterialEdit
    {
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
    }
}
