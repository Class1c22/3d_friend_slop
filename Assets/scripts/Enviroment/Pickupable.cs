using Photon.Pun;
using UnityEngine;

// Повісити на будь-який об'єкт, який гравець зможе підняти.
// Об'єкт МАЄ мати PhotonView (Ownership Transfer = Takeover в інспекторі,
// щоб MasterClient міг передати власність тому, хто підняв предмет) і бути
// заспавненим через PhotonNetwork.Instantiate (або бути сценним об'єктом
// з власним PhotonView, якщо він лежить у сцені зазделегіть).
[RequireComponent(typeof(Collider), typeof(PhotonView))]
public class Pickupable : MonoBehaviourPun
{
    [Tooltip("Чи зараз предмет належить гравцю (в руці АБО в інвентарі, не в світі)")]
    public bool isHeld = false;

    public Sprite icon;
    public Transform customAttachPoint;
    public Vector3 attachPositionOffset = Vector3.zero;
    public Vector3 attachRotationOffset = Vector3.zero;
    public string equipAnimTrigger;

    [Tooltip("Тільки для риби: ідентифікатор виду (напр. \"fish1\"..\"fish5\"). Однакове значення виставити вручну на кожному з 5 префабів риби - за ним акула визначає, чи любить саме цей вид (SharkController.allFishSpeciesIds).")]
    public string fishSpeciesId;

    private Rigidbody rb;
    private Collider col;
    private Quaternion defaultWorldRotation;
    private Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        defaultWorldRotation = transform.rotation;
        originalScale = transform.localScale;
    }

    // ---------------------------------------------------------------
    // МЕРЕЖЕВИЙ ФЛОУ ПІДБОРУ
    //
    // 1. PlayerPickup.TryPickUpNearby() шле RPC_RequestPickup лише на
    //    MasterClient (RpcTarget.MasterClient) - єдина точка арбітражу,
    //    тому навіть якщо двоє гравців тиснуть E в один кадр по одному
    //    предмету, MasterClient обробляє запити послідовно і перемагає
    //    той, чий RPC прийшов першим.
    // 2. MasterClient перевіряє isHeld, і якщо предмет вільний - одразу
    //    позначає isHeld = true (щоб відхилити наступний запит-гонку),
    //    передає PhotonView-власність гравцю-переможцю і розсилає всім
    //    RPC_ConfirmPickup з ID переможця.
    // 3. RPC_ConfirmPickup виконується на КОЖНОМУ клієнті: якщо це "мій"
    //    гравець - реально кладе предмет у руку/інвентар; якщо чужий -
    //    просто ховає предмет зі сцени (Store), щоб усі бачили однакову
    //    картину світу.
    // ---------------------------------------------------------------

    [PunRPC]
    public void RPC_RequestPickup(int requesterViewId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return; // на випадок якщо RpcTarget сплутали
        if (isHeld) return; // хтось уже забрав - гонка програна

        isHeld = true; // застовпили негайно, до фактичного Store()

        PhotonView requesterView = PhotonView.Find(requesterViewId);
        if (requesterView == null)
        {
            isHeld = false; // запитувач зник (вийшов з кімнати) - повертаємо предмет у гру
            return;
        }

        photonView.TransferOwnership(requesterView.Owner);
        photonView.RPC(nameof(RPC_ConfirmPickup), RpcTarget.All, requesterViewId);
    }

    [PunRPC]
    public void RPC_ConfirmPickup(int requesterViewId)
    {
        isHeld = true;

        PhotonView requesterView = PhotonView.Find(requesterViewId);
        if (requesterView == null) return;

        if (requesterView.IsMine)
        {
            PlayerPickup pickup = requesterView.GetComponent<PlayerPickup>();
            pickup.ConfirmPickup(this);
        }
        else
        {
            // Чужий гравець підняв предмет - у нас на екрані просто ховаємо
            // його зі світу (жодних локальних наслідків для нашого інвентаря).
            Store();
        }
    }

    [PunRPC]
    public void RPC_Drop(Vector3 dropWorldPosition, Vector3 throwVelocity)
    {
        Drop(dropWorldPosition, throwVelocity);
    }

    // ---------------------------------------------------------------
    // Локальна механіка (без змін відносно оригіналу) - викликається
    // тільки зсередини RPC-обробників вище, ніколи напряму ззовні.
    // ---------------------------------------------------------------

    public void PickUp(Transform handAttachPoint)
    {
        gameObject.SetActive(true);
        isHeld = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        if (col != null)
            col.enabled = false;

        transform.SetParent(handAttachPoint, false);
        transform.localPosition = attachPositionOffset;
        transform.localRotation = Quaternion.Euler(attachRotationOffset);
        transform.localScale = originalScale;
    }

    public void Store()
    {
        isHeld = true;

        transform.SetParent(null, false);
        transform.localScale = originalScale;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        if (col != null)
            col.enabled = false;

        gameObject.SetActive(false);
    }

    public void Drop(Vector3 dropWorldPosition, Vector3 throwVelocity = default)
    {
        gameObject.SetActive(true);
        isHeld = false;

        transform.SetParent(null, false);
        transform.position = dropWorldPosition;
        transform.rotation = defaultWorldRotation;
        transform.localScale = originalScale;

        if (col != null)
            col.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = throwVelocity;
        }
    }
}