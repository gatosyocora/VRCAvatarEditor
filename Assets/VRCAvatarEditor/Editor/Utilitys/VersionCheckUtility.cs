using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace VRCAvatarEditor.Utilitys
{
    public class VersionCheckUtility
    {
        public class GitHubData
        {
            public string tag_name;
        }

        public static async Task<string> GetLatestVersionFromRemote(string githubReleaseApiUrl)
        {
            var request = UnityWebRequest.Get(githubReleaseApiUrl);
            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return string.Empty;
            }
            else
            {
                var jsonData = request.downloadHandler.text;
                return JsonUtility.FromJson<GitHubData>(jsonData)?.tag_name ?? string.Empty;
            }
        }

        public static bool IsLatestVersion(string local, string remote)
        {
            var localVersion = local.Substring(1).Split('.').Select(x => int.Parse(x)).ToArray();
            var remoteVersion = remote.Substring(1).Split('.').Select(x => int.Parse(x)).ToArray();

            // サイズを合わせる
            if (localVersion.Length < remoteVersion.Length)
            {
                localVersion = Enumerable.Range(0, remoteVersion.Length)
                                    .Select(i =>
                                    {
                                        if (i < localVersion.Length) return localVersion[i];
                                        else return 0;
                                    })
                                    .ToArray();
            }
            else if (localVersion.Length > remoteVersion.Length)
            {
                remoteVersion = Enumerable.Range(0, localVersion.Length)
                                    .Select(i =>
                                    {
                                        if (i < remoteVersion.Length) return remoteVersion[i];
                                        else return 0;
                                    })
                                    .ToArray();
            }

            for (int index = 0; index < localVersion.Length; index++)
            {
                var l = localVersion[index];
                var r = remoteVersion[index];
                if (l < r) return false;
                if (l > r) return true;
            }
            return true;
        }
    }
}