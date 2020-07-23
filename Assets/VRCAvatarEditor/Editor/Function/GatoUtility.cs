using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public static List<Material> GetMaterials(GameObject obj)
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

        return materials;
    }
}
