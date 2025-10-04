using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyScript : MonoBehaviour
{
    [Header("UI References")]
    public Button createLobbyButton;
    public TextMeshProUGUI lobbyCodeDisplay;
    public TextMeshProUGUI connectionStatusText;
    public NetworkScript networkScript;

    void Start()
    {
        // Auto-find NetworkScript if not assigned
        if (networkScript == null)
        {
            networkScript = FindFirstObjectByType<NetworkScript>();
        }

        // Assign UI references to NetworkScript
        if (networkScript != null)
        {
            networkScript.createLobbyButton = createLobbyButton;
            networkScript.lobbyCodeDisplay = lobbyCodeDisplay;
            networkScript.connectionStatusText = connectionStatusText;
        }
        else
        {
            Debug.LogError("NetworkScript not found! Make sure NetworkScript is in the scene.");
        }
    }
}