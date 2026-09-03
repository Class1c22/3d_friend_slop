using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Обробляє натискання кнопки "New Game" у Game Over UI: перезавантажує
/// ВСЮ сцену для ВСІХ гравців через PhotonNetwork.LoadLevel, що автоматично
/// скидає острів, акулу, риб, укуси - все, без ручного відновлення кожної
/// окремої системи.
///
/// Повісити на порожній GameObject у сцені (напр. "GameManager") ЯК SCENE
/// OBJECT - PhotonView на ньому має бути звичайним "Scene"-об'єктом (не
/// спавниться рантайм), тоді ним автоматично володіє поточний MasterClient
/// (так само, як HeightmapIsland).
///
/// У кнопці "new game" (Button -> On Click()) признач цей об'єкт і метод
/// RestartGame().
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class GameRestartManager : MonoBehaviourPun
{
    public void RestartGame()
    {
        // Будь-який гравець (навіть не MasterClient) може натиснути кнопку -
        // тому запит на рестарт відправляємо через RPC саме MasterClient'у,
        // а вже він виконує PhotonNetwork.LoadLevel (це можна робити лише
        // з MasterClient - AutomaticallySyncScene подбає, щоб усі клієнти
        // перейшли в нову сцену синхронно).
        photonView.RPC(nameof(RPC_RequestRestart), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_RequestRestart()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("[GameRestartManager] Перезавантажую сцену для всіх гравців...");
        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex);
    }
}