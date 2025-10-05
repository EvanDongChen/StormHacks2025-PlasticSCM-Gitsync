using UnityEngine;
using TMPro;

public class ChatSpawner : MonoBehaviour
{
    public GameObject chatTextPrefab;
    public Transform spawnPoint;

    public void SpawnChat(string message)
    {
        GameObject chatText = Instantiate(chatTextPrefab, spawnPoint.position, Quaternion.identity);
        TextMeshProUGUI tmp = chatText.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = message;
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI component not found in chatTextPrefab.");
        }

        chatText.AddComponent<ChatText>();

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayRandomSFX();
        }
        else
        {
            Debug.LogWarning("SFXManager instance not found in scene.");
        }
    }

    
}