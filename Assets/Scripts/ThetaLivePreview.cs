using System.Collections;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace ThetaXSample
{
    /// <summary>
    /// Thetaライブビューサンプル
    /// </summary>
    public class ThetaLivePreview : MonoBehaviour
    {
        // RICOH THETAのIPアドレス（通常は192.168.1.1）
        private string thetaIp = "http://192.168.1.1";

        // ユーザー名とパスワード（THETAのデフォルトは通常"THETA"）
        private string username = "THETA";
        private string password = "YOUR_PASSWORD"; // カメラのWi-Fiパスワード

        void Start()
        {
            StartCoroutine(GetLivePreview());
        }

        IEnumerator GetLivePreview()
        {
            // Step 1: Nonceを取得
            UnityWebRequest initialRequest = UnityWebRequest.Get($"{thetaIp}/osc/commands/execute");
            yield return initialRequest.SendWebRequest();

            if (initialRequest.responseCode == 401)
            {
                string authHeader = initialRequest.GetResponseHeader("WWW-Authenticate");
                string nonce = ExtractNonce(authHeader);

                // Step 2: Digest認証レスポンスを計算
                string ha1 = CalculateMD5Hash($"{username}:THETA:{password}");
                string ha2 = CalculateMD5Hash($"POST:/osc/commands/execute");
                string response = CalculateMD5Hash($"{ha1}:{nonce}:{ha2}");

                // 認証ヘッダーを作成
                string authorizationHeader = $"Digest username=\"{username}\", realm=\"THETA\", nonce=\"{nonce}\", uri=\"/osc/commands/execute\", response=\"{response}\"";

                // Step 3: getLivePreviewを実行
                UnityWebRequest previewRequest = new UnityWebRequest($"{thetaIp}/osc/commands/execute", "POST");
                string jsonData = "{\"name\": \"camera.getLivePreview\"}";
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                previewRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                previewRequest.downloadHandler = new DownloadHandlerBuffer();
                previewRequest.SetRequestHeader("Content-Type", "application/json");
                previewRequest.SetRequestHeader("Authorization", authorizationHeader);

                yield return previewRequest.SendWebRequest();

                if (previewRequest.result == UnityWebRequest.Result.Success)
                {
                    // ライブプレビュー映像を処理
                    byte[] previewData = previewRequest.downloadHandler.data;
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(previewData);
                    // ライブプレビュー映像を画面に表示
                    GetComponent<Renderer>().material.mainTexture = texture;
                    Debug.Log("Successfully obtained live preview.");
                }
                else
                {
                    Debug.Log($"Failed to obtain live preview: {previewRequest.responseCode}");
                    Debug.Log(previewRequest.error);
                }
            }
            else
            {
                Debug.Log("Initial request failed: " + initialRequest.error);
            }
        }

        private string ExtractNonce(string authHeader)
        {
            // Extract nonce value from the WWW-Authenticate header
            int nonceIndex = authHeader.IndexOf("nonce=\"") + 7;
            int nonceEndIndex = authHeader.IndexOf("\"", nonceIndex);
            return authHeader.Substring(nonceIndex, nonceEndIndex - nonceIndex);
        }

        private string CalculateMD5Hash(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}