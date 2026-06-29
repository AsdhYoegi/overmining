using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Customer : NetworkBehaviour
{
    public float moveSpeed = 3f;
    public string requestedItemName = "Rock"; // 要求するアイテム
    public float waitTime = 10f; // 待機時間
    
    // 吹き出しUI用
    public GameObject bubbleObj;
    public TextMeshProUGUI bubbleText; 
    
    public int queueIndex = 0; // 行列の何番目か

    private Vector3 targetPos;
    private bool isAtShelf = false;
    private bool isLeaving = false;
    private float currentWaitTime = 0f;

    public void UpdateQueueIndex(int newIndex)
    {
        queueIndex = newIndex;
        isAtShelf = false; // また前へ進む
        if (DisplayShelf.Instance != null)
        {
            // 棚の手前 2m ずつ間隔をあけて並ぶ
            targetPos = DisplayShelf.Instance.transform.position + new Vector3(0, 0, -2f - (queueIndex * 2f));
        }
    }

    public override void OnNetworkSpawn()
    {
        UpdateQueueIndex(queueIndex); // 初期位置の計算
        if (bubbleObj != null) bubbleObj.SetActive(false);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (isLeaving)
        {
            // 帰る処理（とりあえず後ろへ向かって歩かせて消す）
            transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(0,0,-10f), moveSpeed * Time.deltaTime);
            currentWaitTime += Time.deltaTime;
            if (currentWaitTime > 5f) GetComponent<NetworkObject>().Despawn();
            return;
        }

        if (!isAtShelf)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                isAtShelf = true;
                UpdateBubbleClientRpc(true, "Want:\n" + requestedItemName);
            }
        }
        else
        {
            // 先頭にいる場合のみ買い物ができる
            if (queueIndex == 0)
            {
                currentWaitTime += Time.deltaTime;
                
                if (DisplayShelf.Instance != null && DisplayShelf.Instance.TrySellItem(requestedItemName, out int price))
                {
                    GameManager.Instance.AddMoney(price);
                    UpdateBubbleClientRpc(true, "Thanks!");
                    Leave();
                }
                else if (currentWaitTime >= waitTime)
                {
                    UpdateBubbleClientRpc(true, "Too slow...");
                    Leave();
                }
            }
            else
            {
                // 並んでいる間は待機時間を進めない
                // UpdateBubbleClientRpc(true, "Waiting...");
            }
        }
    }

    private void Leave()
    {
        isLeaving = true;
        currentWaitTime = 0f;
        
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.RemoveCustomer(this);
        }
    }

    [ClientRpc]
    private void UpdateBubbleClientRpc(bool show, string text)
    {
        if (bubbleObj != null) bubbleObj.SetActive(show);
        if (bubbleText != null) bubbleText.text = text;
    }
}
