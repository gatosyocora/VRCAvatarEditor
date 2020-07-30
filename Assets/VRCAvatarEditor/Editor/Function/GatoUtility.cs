using Amazon.Auth.AccessControlPolicy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace VRCAvatarEditor
{
    public static class GatoUtility
    {
        /// <summary>
        /// 指定オブジェクト以下のSkinnedMeshRendererのリストを取得する
        /// </summary>
        /// <param name="parentObj">親オブジェクト</param>
        /// <returns>SkinnedMeshRendererのリスト</returns>
        public static List<SkinnedMeshRenderer> GetSkinnedMeshList(GameObject parentObj)
        {
            var skinnedMeshList = new List<SkinnedMeshRenderer>();

            var skinnedMeshes = parentObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var skinnedMesh in skinnedMeshes)
            {
                skinnedMeshList.Add(skinnedMesh);
            }

            return skinnedMeshList;
        }

        /// <summary>
        /// 指定オブジェクト以下のMeshRendererのリストを取得する
        /// </summary>
        /// <param name="parentObj">親オブジェクト</param>
        /// <returns>MeshRendererのリスト</returns>
        public static List<MeshRenderer> GetMeshList(GameObject parentObj)
        {
            var meshList = new List<MeshRenderer>();

            var meshes = parentObj.GetComponentsInChildren<MeshRenderer>(true);

            foreach (var mesh in meshes)
            {
                meshList.Add(mesh);
            }

            return meshList;
        }

        /// <summary>
        /// obj以下のすべてのポリゴン数を取得する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetAllTrianglesCount(GameObject obj, ref int countInactive)
        {
            int count = 0;
            countInactive = 0;

            var skinnedMeshList = GetSkinnedMeshList(obj);
            var meshList = GetMeshList(obj);

            if (skinnedMeshList != null)
            {
                foreach (var skinnedMeshRenderer in skinnedMeshList)
                {
                    if (skinnedMeshRenderer.sharedMesh == null) continue;

                    if (skinnedMeshRenderer.gameObject.activeSelf)
                        count += skinnedMeshRenderer.sharedMesh.triangles.Length / 3;
                    else
                        countInactive += skinnedMeshRenderer.sharedMesh.triangles.Length / 3;
                }
            }

            if (meshList != null)
            {
                foreach (var meshRenderer in meshList)
                {
                    var meshFilter = meshRenderer.gameObject.GetComponent<MeshFilter>();
                    if (meshFilter == null) continue;
                    else if (meshFilter.sharedMesh == null) continue;

                    if (meshFilter.gameObject.activeSelf)
                        count += meshFilter.sharedMesh.triangles.Length / 3;
                    else
                        countInactive += meshFilter.sharedMesh.triangles.Length / 3;
                }
            }

            return count;
        }

        /// <summary>
        /// obj以下のメッシュに設定されたマテリアルを全て取得する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Material[] GetMaterials(GameObject obj)
        {
            var materials = new List<Material>();

            var skinnedMeshes = GetSkinnedMeshList(obj);
            var meshes = GetMeshList(obj);

            foreach (var skinnedMesh in skinnedMeshes)
            {
                foreach (var mat in skinnedMesh.sharedMaterials)
                {
                    materials.Add(mat);
                }
            }

            foreach (var mesh in meshes)
            {
                foreach (var mat in mesh.sharedMaterials)
                {
                    materials.Add(mat);
                }
            }

            materials = materials.Distinct().ToList<Material>();

            return materials.ToArray();
        }

        /// <summary>
        /// AnimationClipにMissingなパスが含まれていないことを検証する
        /// </summary>
        /// <param name="animator">AnimationClipを設定するAnimator</param>
        /// <param name="clip">検証するAnimationClip</param>
        /// <returns>Missingなパスが含まれていないか</returns>
        public static bool ValidateMissingPathInAnimationClip(Animator animator, AnimationClip clip)
        {
            var transform = animator.transform;
            foreach (var animationPath in AnimationUtility.GetCurveBindings(clip).Select(p => p.path))
            {
                if (transform.Find(animationPath) is null) return false;
            }
            return true;
        }

        /// <summary>
        /// Missingなパスを自動修正する（対象オブジェクトと同じ名前のオブジェクトが複数あった場合, 自動修正に失敗する）
        /// </summary>
        /// <param name="animator">clipを設定するAnimator</param>
        /// <param name="clip">Missingなパスを含むAnimationClip</param>
        /// <returns>Missingなパスを含まないか</returns>
        public static bool TryFixMissingPathInAnimationClip(Animator animator, AnimationClip clip)
        {
            var result = true;
            var rootTransform = animator.transform;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                // MissingなPathを含むbindingに対してのみおこなう
                var animationPath = binding.path;
                if (rootTransform.Find(animationPath) != null) continue;

                var targetObjectName = Path.GetFileName(animationPath);
                // 特定の名前を持つ子オブジェクトの一覧を取得
                var targetTransforms = rootTransform.GetComponentsInChildren<Transform>()
                                            .Where(t => t.name == targetObjectName);

                // 1つだけ見つかったときのみ自動修正
                if (targetTransforms.Count() == 1)
                {
                    var targetTransform = targetTransforms.Single();
                    var newBinding = new EditorCurveBinding
                    {
                        path = AnimationUtility.CalculateTransformPath(targetTransforms.Single(), rootTransform),
                        propertyName = binding.propertyName,
                        type = binding.type
                    };
                    // Pathを再設定
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    AnimationUtility.SetEditorCurve(clip, newBinding, curve);
                    continue;
                }
                result = false;
            }
            // いずれかのBindingでMissingなパスが修正できなかった場合, falseになる
            return result;
        }

        /// <summary>
        /// Materialを複製する
        /// </summary>
        /// <param name="srcMaterial">複製するMaterial</param>
        /// <returns>複製されたMaterial</returns>
        public static Material DuplicateMaterial(Material srcMaterial)
        {
            var originalPath = AssetDatabase.GetAssetPath(srcMaterial);
            var newPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);
            AssetDatabase.CopyAsset(originalPath, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Material>(newPath);
        }

        /// <summary>
        /// Materialを複製する
        /// </summary>
        /// <param name="srcMaterial">複製するMaterial</param>
        /// <returns>複製されたMaterial</returns>
        public static Material[] DuplicateMaterials(Material[] srcMaterials)
        {
            var paths = new string[srcMaterials.Length];
            for (int i = 0; i < srcMaterials.Length; i++)
            {
                var originalPath = AssetDatabase.GetAssetPath(srcMaterials[i]);
                var newPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);
                AssetDatabase.CopyAsset(originalPath, newPath);
                paths[i] = newPath;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var newMaterials = new Material[srcMaterials.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                newMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
            }
            return newMaterials;
        }

        /// <summary>
        /// Materialを置き換える
        /// </summary>
        /// <param name="rootObject">ルートオブジェクト</param>
        /// <param name="srcMaterial">変更前のMaterial</param>
        /// <param name="dstMaterial">変更後のMaterial</param>
        public static void ReplaceMaterial(GameObject rootObject, Material srcMaterial, Material dstMaterial)
        {
            foreach (var renderer in rootObject.GetComponentsInChildren<Renderer>().ToArray())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] != srcMaterial) continue;

                    materials[i] = dstMaterial;
                }
                Undo.RecordObject(renderer, "Replace Materials");
                renderer.sharedMaterials = materials;
            }
        }

        // TODO: builtinShaderの取得がうまくできていない
        public static Shader[] LoadShadersInProject()
        {
            var shaderList = new List<Shader>();
            // 2018.3以降では空配列が返ってくるらしい
            //var builtinShaders = AssetDatabase.LoadAllAssetsAtPath("Resource/unity_builtin_extra").OfType<Shader>();
            var builtinShaders = LoadBuiltinShaders();
            shaderList.AddRange(builtinShaders);
            var resourceShaders = Resources.FindObjectsOfTypeAll<Shader>();
            shaderList.AddRange(Resources.FindObjectsOfTypeAll<Shader>());
            var customShaders = AssetDatabase.FindAssets("t:shader")
                                    .Select(g => AssetDatabase.GUIDToAssetPath(g))
                                    .Select(p => AssetDatabase.LoadAssetAtPath<Shader>(p));
            shaderList.AddRange(customShaders);

            // 重複, Hiddenは含めない
            return shaderList.Distinct().Where(s => s.name.Split('/').First() != "Hidden").OrderBy(s => s.name).ToArray();
        }

        private static Shader[] LoadBuiltinShaders()
        {
            return new Shader[]
            {
            Shader.Find("AR/TangoARRender"),
            Shader.Find("Autodesk Interactive"),
            Shader.Find("FX/Flare"),
            //Shader.Find("Legacy Shaders/Bumped Diffuse"),
            //Shader.Find("Legacy Shaders/Bumped Specular"),
            //Shader.Find("Legacy Shaders/Decal"),
            //Shader.Find("Legacy Shaders/Diffuse"),
            //Shader.Find("Legacy Shaders/Diffuse Detail"),
            //Shader.Find("Legacy Shaders/Diffuse Fast"),
            //Shader.Find("Legacy Shaders/Lightmapped/Bumped Diffuse"),
            //Shader.Find("Legacy Shaders/Lightmapped/Bumped Specular"),
            //Shader.Find("Legacy Shaders/Lightmapped/Diffuse"),
            //Shader.Find("Legacy Shaders/Lightmapped/Specular"),
            //Shader.Find("Legacy Shaders/Lightmapped/VertexLit"),
            //Shader.Find("Legacy Shaders/Parallax Diffuse"),
            //Shader.Find("Legacy Shaders/Parallax Specular"),
            //Shader.Find("Legacy Shaders/Particles/Additive"),
            //Shader.Find("Legacy Shaders/Particles/Additive (Soft)"),
            //Shader.Find("Legacy Shaders/Particles/Alpha Blended"),
            //Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"),
            //Shader.Find("Legacy Shaders/Particles/Anim Alpha Blended"),
            //Shader.Find("Legacy Shaders/Particles/Multiply"),
            //Shader.Find("Legacy Shaders/Particles/Multiply (Double)"),
            //Shader.Find("Legacy Shaders/Particles/VertexLit Blended"),
            //Shader.Find("Legacy Shaders/Particles/~Additive-Multiply"),
            //Shader.Find("Legacy Shaders/Particles/~Additive-Multiply"),
            //Shader.Find("Legacy Shaders/Reflective/Bumped Diffuse"),
            //Shader.Find("Legacy Shaders/Reflective/Bumped Specular"),
            //Shader.Find("Legacy Shaders/Reflective/Bumped Unlit"),
            //Shader.Find("Legacy Shaders/Reflective/Bumped VertexLit"),
            //Shader.Find("Legacy Shaders/Reflective/Diffuse"),
            //Shader.Find("Legacy Shaders/Reflective/Parallax Diffuse"),
            //Shader.Find("Legacy Shaders/Reflective/Parallax Specular"),
            //Shader.Find("Legacy Shaders/Reflective/Specular"),
            //Shader.Find("Legacy Shaders/Reflective/VertexLit"),
            //Shader.Find("Legacy Shaders/Self-Illumin/Bumped Diffuse"),
            //Shader.Find("Legacy Shaders/Self-Illumin/Bumped Specular"),
            // ...
            Shader.Find("Mobile/Bumped Diffuse"),
            Shader.Find("Mobile/Bumped Specular"),
            Shader.Find("Mobile/Bumped Specular (1 Directional Realtime Light)"),
            Shader.Find("Mobile/Diffuse"),
            Shader.Find("Mobile/Particles/Additive"),
            Shader.Find("Mobile/Particles/Alpha Blended"),
            Shader.Find("Mobile/Particles/Multiply"),
            Shader.Find("Mobile/Particles/VertexLit Blended"),
            Shader.Find("Mobile/Skybox"),
            Shader.Find("Mobile/Unlit (Supports Lightmap)"),
            Shader.Find("Mobile/VertexLit"),
            Shader.Find("Mobile/VertexLit (Only Directional Lights)"),
            Shader.Find("Nature/SpeedTree"),
            Shader.Find("Nature/SpeedTree Billboard"),
            Shader.Find("Nature/SpeedTree8"),
            Shader.Find("Nature/Terrain/Diffuse"),
            Shader.Find("Nature/Terrain/Specular"),
            Shader.Find("Nature/Terrain/Standard"),
            Shader.Find("Nature/Tree Creator Bark"),
            Shader.Find("Nature/Tree Creator Leaves"),
            Shader.Find("Nature/Tree Creator Leaves Fast"),
            Shader.Find("Nature/Tree Soft Occlusion Bark"),
            Shader.Find("Nature/Tree Soft Occlusion Leaves"),
            Shader.Find("Particles/Standard Surface"),
            Shader.Find("Particles/Standard Unlit"),
            Shader.Find("Skybox/6 Sided"),
            Shader.Find("Skybox/Cubemap"),
            Shader.Find("Skybox/Panoramic"),
            Shader.Find("Skybox/Procedural"),
            Shader.Find("Sprites/Default"),
            Shader.Find("Sprites/Diffuse"),
            Shader.Find("Sprites/Mask"),
            Shader.Find("Standard"),
            Shader.Find("Standard (Specular setup)"),
            Shader.Find("UI/Default"),
            Shader.Find("UI/Default Font"),
            Shader.Find("UI/DefaultETC1"),
            Shader.Find("UI/Lit/Bumped"),
            Shader.Find("UI/Lit/Detail"),
            Shader.Find("UI/Lit/Refraction"),
            Shader.Find("UI/Lit/Refraction Detail"),
            Shader.Find("UI/Lit/Transparent"),
            Shader.Find("UI/Unlit/Detail"),
            Shader.Find("UI/Unlit/Text"),
            Shader.Find("UI/Unlit/Text Detail"),
            Shader.Find("UI/Unlit/Transparent"),
            Shader.Find("Unlit/Color"),
            Shader.Find("Unlit/Texture"),
            Shader.Find("Unlit/Transparent"),
            Shader.Find("Unlit/Transparent Cutout"),
            Shader.Find("VR/SpatialMapping/Occlusion"),
            Shader.Find("VR/SpatialMapping/Wireframe")
            };
        }
    }
}
