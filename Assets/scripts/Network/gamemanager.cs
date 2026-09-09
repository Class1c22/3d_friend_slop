using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Обробляє натискання кнопки "New Game": замість PhotonView.RPC (який
/// вимагає коректний Scene ViewID і ламається, якщо ID не забекався)
/// використовує PhotonNetwork.RaiseEvent - подію без прив'язки до
/// конкретного GameObject/ViewID. Надійніше для об'єктів, які не
/// гарантовано мають стабільний ViewID.
/// </summary>
public class GameRestartManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte RestartRequestEventCode = 1;

    private void OnEnable()  => PhotonNetwork.AddCallbackTarget(this);
    private void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);

    public void RestartGame()
    {
        Debug.Log($"[GameRestartManager] RestartGame() викликано кнопкою. " +
                  $"IsConnected={PhotonNetwork.IsConnected}, InRoom={PhotonNetwork.InRoom}, " +
                  $"IsMasterClient={PhotonNetwork.IsMasterClient}");

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[GameRestartManager] Не в кімнаті Photon - подію не буде відправлено!");
            return;
        }

        // Надсилаємо подію MasterClient'у (працює навіть якщо натиснув не сам мастер)
        var options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        var sendOptions = SendOptions.SendReliable;

        PhotonNetwork.RaiseEvent(RestartRequestEventCode, null, options, sendOptions);
        Debug.Log("[GameRestartManager] Подію RestartRequest відправлено MasterClient'у.");
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != RestartRequestEventCode) return;

        Debug.Log($"[GameRestartManager] Отримано RestartRequest. IsMasterClient={PhotonNetwork.IsMasterClient}");

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[GameRestartManager] Цей клієнт не MasterClient - ігнорую.");
            return;
        }

        Debug.Log("[GameRestartManager] Перезавантажую сцену для всіх гравців...");
        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex);
    }
}