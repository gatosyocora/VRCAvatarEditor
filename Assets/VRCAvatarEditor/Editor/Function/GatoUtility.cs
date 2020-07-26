using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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
}
