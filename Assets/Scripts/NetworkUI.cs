using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkUI : MonoBehaviour
{
    private void Awake()
    {
        var networkManager = GetComponent<NetworkManager>();

        // Resourcesフォルダ内の全てのプレハブをロードし、NetworkObjectが付いているものを自動登録
        if (networkManager != null)
        {
            GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>("");
            foreach (var prefab in loadedPrefabs)
            {
                NetworkObject netObj = prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    // 既に登録されているかチェックして重複登録を防ぐ
                    bool alreadyRegistered = false;
                    foreach(var p in networkManager.NetworkConfig.Prefabs.Prefabs) {
                        if (p.Prefab == prefab) alreadyRegistered = true;
                    }
                    if (!alreadyRegistered)
                    {
                        networkManager.AddNetworkPrefab(prefab);
                    }
                }
            }
        }

        // NetworkManagerにTransportが設定されていない場合の自動割り当て
        if (networkManager != null && networkManager.NetworkConfig.NetworkTransport == null)
        {
            var transport = GetComponent<UnityTransport>();
            if (transport != null)
            {
                networkManager.NetworkConfig.NetworkTransport = transport;
            }
        }
    }

    private void Start()
    {
        // テストプレイ用に、起動時に自動でホストとして開始する
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("自動的にホストとして開始しました！");
        }
    }
}
