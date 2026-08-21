using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// Цей скрипт підключає гру до Photon Cloud (мережа + голос одночасно)
// і спавнить гравця в спільній кімнаті.
//
// НАЛАШТУВАННЯ:
// 1. Перенеси свій префаб гравця (mainhero_animated) у папку Assets/Resources/
//    (створи цю папку, якщо її немає) — PhotonNetwork.Instantiate вимагає,
//    щоб префаб лежав саме в Resources.
// 2. Створи порожній GameObject у сцені, назви його "NetworkManager",
//    і додай на нього цей скрипт.
// 3. У полі Player Prefab Name встав точну назву префабу з Resources.

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Назва префабу гравця (файл має лежати в Assets/Resources/)")]
    [SerializeField] private string playerPrefabName = "mainhero_animated";

    [Header("Назва кімнати (усі гравці з однаковою назвою потраплять разом)")]
    [SerializeField] private string roomName = "MainRoom";

    private void Start()
    {
        Debug.Log("Підключення до Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Підключено до Photon Master Server. Приєднуюсь до кімнати...");
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions(), TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Зайшов у кімнату '{roomName}'. Гравців у кімнаті: {PhotonNetwork.CurrentRoom.PlayerCount}");

        // Спавнимо гравця в довільній точці (0,0,0) — заміни на свою точку спавну
        Vector3 spawnPosition = new Vector3(0f, 0f, 0f);
        PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Відключено від Photon. Причина: {cause}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Не вдалось зайти в кімнату: {message}");
    }
}