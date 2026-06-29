using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class CustomerManager : NetworkBehaviour
{
    public static CustomerManager Instance { get; private set; }
    public GameObject customerPrefab;
    public Transform spawnPoint; // お客さんが出てくる場所
    public float minSpawnTime = 3f;
    public float maxSpawnTime = 8f;
    
    private float spawnTimer = 0f;
    public List<Customer> activeCustomers = new List<Customer>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (!IsServer) return;
        
        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase.Value == GameManager.GamePhase.Mining)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                spawnTimer = Random.Range(minSpawnTime, maxSpawnTime);
                SpawnCustomer();
            }
        }
    }

    private void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null) return;
        if (DisplayShelf.Instance == null) return; // 商品棚がないと行列が作れない
        
        GameObject customerObj = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        Customer cust = customerObj.GetComponent<Customer>();
        NetworkObject netObj = customerObj.GetComponent<NetworkObject>();
        if (netObj != null && cust != null)
        {
            activeCustomers.Add(cust);
            cust.queueIndex = activeCustomers.Count - 1; // 自分の並び順
            netObj.Spawn();
        }
    }

    public void RemoveCustomer(Customer cust)
    {
        if (activeCustomers.Contains(cust))
        {
            activeCustomers.Remove(cust);
            // 残りの人を1つ前に進める
            for (int i = 0; i < activeCustomers.Count; i++)
            {
                activeCustomers[i].UpdateQueueIndex(i);
            }
        }
    }
}
