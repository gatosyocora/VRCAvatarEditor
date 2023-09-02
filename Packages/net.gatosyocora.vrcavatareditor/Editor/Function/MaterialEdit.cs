using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRCAvatar = VRCAvatarEditor.Base.VRCAvatarBase;

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
        public static void ReplaceMaterial(VRCAvatar avatar, Material srcMaterial, Material dstMaterial)
        {
            GatoUtility.ReplaceMaterial(avatar.Animator.gameObject, srcMaterial, dstMaterial);
            var index = Array.IndexOf(avatar.Materials, srcMaterial);
            if (index == -1) return;
            avatar.Materials[index] = dstMaterial;
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


		/*
			unity-material-cleaner
			Copyright (c) 2019 ina-amagami (ina@amagamina.jp)
			This software is released under the MIT License.
			https://opensource.org/licenses/mit-license.php
		*/
		/// <summary>
		/// マテリアルの不要なプロパティを削除する
		/// </summary>
		/// <param name="mat">対象のマテリアル</param>
		/// <param name="path">マテリアルが保存されているパス</param>
		public static void DeleteUnusedProperties(Material mat, string path)
		{
			// 不要なパラメータを削除する方法として、新しいマテリアルを作ってそこに必要なパラメータだけコピーする
			var newMat = new Material(mat.shader);

			// パラメータのコピー
			newMat.name = mat.name;
			newMat.renderQueue = (mat.shader.renderQueue == mat.renderQueue) ? -1 : mat.renderQueue;
			newMat.enableInstancing = mat.enableInstancing;
			newMat.doubleSidedGI = mat.doubleSidedGI;
			newMat.globalIlluminationFlags = mat.globalIlluminationFlags;
			newMat.hideFlags = mat.hideFlags;
			newMat.shaderKeywords = mat.shaderKeywords;

			// Propertiesのコピー
			var properties = MaterialEditor.GetMaterialProperties(new Material[] { mat });
			for (int pIdx = 0; pIdx < properties.Length; ++pIdx)
			{
				SetPropertyToMaterial(newMat, properties[pIdx]);
			}

			// GUIDが変わらないように置き換える
			string tempPath = path + "_temp";
			AssetDatabase.CreateAsset(newMat, tempPath);
			FileUtil.ReplaceFile(tempPath, path);
			AssetDatabase.DeleteAsset(tempPath);
		}

		/// <summary>
		/// MaterialPropertyを解釈してMaterialに設定する
		/// </summary>
		/// <param name="mat">対象のマテリアル</param>
		/// <param name="property">設定するプロパティ</param>
		public static void SetPropertyToMaterial(Material mat, MaterialProperty property)
		{
			switch (property.type)
			{
				case MaterialProperty.PropType.Color:
					mat.SetColor(property.name, property.colorValue);
					break;

				case MaterialProperty.PropType.Float:
				case MaterialProperty.PropType.Range:
					mat.SetFloat(property.name, property.floatValue);
					break;

				case MaterialProperty.PropType.Texture:
					// PerRenderDataの場合はnullになるようにする
					Texture tex = null;
					if ((property.flags & MaterialProperty.PropFlags.PerRendererData) != MaterialProperty.PropFlags.PerRendererData)
					{
						tex = property.textureValue;
					}
					mat.SetTexture(property.name, tex);
					if ((property.flags & MaterialProperty.PropFlags.NoScaleOffset) != MaterialProperty.PropFlags.NoScaleOffset)
					{
						mat.SetTextureScale(property.name, new Vector2(property.textureScaleAndOffset.x, property.textureScaleAndOffset.y));
						mat.SetTextureOffset(property.name, new Vector2(property.textureScaleAndOffset.z, property.textureScaleAndOffset.w));
					}
					break;

				case MaterialProperty.PropType.Vector:
					mat.SetVector(property.name, property.vectorValue);
					break;
			}
		}

		public static Shader CalculateSimilarShader(IList<Shader> shaders, Shader srcShader)
		{
			var srcShaderType = GetShaderType(srcShader);

			var dstShaders = shaders.Select((s, i) => new { Value = s, Index = i, Type = GetShaderType(s) });
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
									.Where(s => s.Type == ShaderType.Opaque)
									.FirstOrDefault().Index;
			}
			// ShaderTypeが一致するShaderが複数見つかった
			else
			{
				// とりあえずShaderTypeが同じShaderの中の一番名前の階層が低いShaderにする
				dstShaderIndex = dstShaders.Where(s => s.Type == srcShaderType).OrderBy(s => s.Value.name.Count(c => c == '/')).First().Index;
			}
			return shaders[dstShaderIndex];
		}
	}
}
