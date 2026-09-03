using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// Цей скрипт підключає гру до Photon Cloud (мережа + голос одночасно),
// спавнить гравця в спільній кімнаті і коректно респавнить його після
// PhotonNetwork.LoadLevel (напр. коли гру перезапускають кнопкою "New Game").
//
// НАЛАШТУВАННЯ:
// 1. Перенеси свій префаб гравця (mainhero_animated) у папку Assets/Resources/.
// 2. Створи порожній GameObject "NetworkManager" у сцені, додай цей скрипт.
// 3. У полі Player Prefab Name встав точну назву префабу з Resources.
// 4. (Новe) Створи порожні GameObject-точки спавну десь у сцені (напр.
//    "SpawnPoint1", "SpawnPoint2") і перетягни їх у масив Spawn Points.
//    Якщо масив лишити порожнім - гравець спавниться в (0,0,0), як і раніше.
// 5. Переконайся, що сцена додана в File -> Build Settings -> Scenes In Build
//    (PhotonNetwork.LoadLevel вимагає, щоб сцена мала build index).
//
// ВАЖЛИВО: переконайся, що в сцені є ТІЛЬКИ ОДИН об'єкт з цим скриптом —
// два NetworkManager в сцені викликають OnJoinedRoom двічі і спавнять
// два персонажі на одного гравця.
public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Назва префабу гравця (файл має лежати в Assets/Resources/)")]
    [SerializeField] private string playerPrefabName = "mainhero_animated";

    [Header("Назва кімнати (усі гравці з однаковою назвою потраплять разом)")]
    [SerializeField] private string roomName = "MainRoom";

    [Header("Точки спавну (можна лишити порожнім - тоді спавн у (0,0,0))")]
    [Tooltip("Якщо точок кілька - обирається випадкова. Якщо одна - завжди вона.")]
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        // Без цього PhotonNetwork.LoadLevel перезавантажить сцену лише в того,
        // хто його викликав (MasterClient) - усі інші лишаться в старій сцені.
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            // Ми вже підключені й у кімнаті - це означає, що сцена щойно
            // перезавантажилась через PhotonNetwork.LoadLevel (наприклад,
            // після натискання "New Game"), а не перший запуск гри.
            // OnConnectedToMaster/OnJoinedRoom вдруге НЕ викличуться (ми і так
            // вже в кімнаті), тому спавнимось напряму тут.
            Debug.Log("Сцена перезавантажена - спавню гравця напряму.");
            SpawnPlayer();
            return;
        }

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
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        // Захист від подвійного спавну ТІЛЬКИ в межах одного й того самого
        // життя сцени: TagObject перевіряємо через Unity-cast, бо після
        // PhotonNetwork.LoadLevel стара посилання-обгортка технічно не null
        // на рівні C#, хоча сам GameObject уже знищений.
        GameObject existing = PhotonNetwork.LocalPlayer.TagObject as GameObject;
        if (existing != null)
        {
            Debug.LogWarning("Гравець вже заспавнений для цього актора - пропускаю повторний Instantiate.");
            return;
        }

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (point != null)
            {
                spawnPosition = point.position;
                spawnRotation = point.rotation;
            }
        }

        GameObject player = PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, spawnRotation);
        PhotonNetwork.LocalPlayer.TagObject = player;
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LocalPlayer.TagObject = null;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Відключено від Photon. Причина: {cause}");
        PhotonNetwork.LocalPlayer.TagObject = null;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Не вдалось зайти в кімнату: {message}");
    }
}