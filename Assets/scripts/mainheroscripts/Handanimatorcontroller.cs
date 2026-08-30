using Photon.Pun;
using UnityEngine;

public class HandAnimatorController : MonoBehaviourPun
{
    [Tooltip("Animator компонент на об'єкті руки (або перетягніть сюди вручну)")]
    public Animator handAnimator;

    public float moveThreshold = 0.1f;
    public float sprintThreshold = 5f;
    public float sprintMultiplier = 2f;
    public KeyCode jumpKey = KeyCode.Space;

    private bool isHolding = false;
    private bool isFishingEquipped = false;
    private bool isJumping = false;
    private bool isGrounded = true;

    void Awake()
    {
        if (handAnimator == null)
            handAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        // Читати локальний Input (WASD/Space) можна ЛИШЕ для свого аватара.
        // Для чужих копій параметри Animator (speed, IsMoving, isJumping тощо)
        // приходять по мережі через Photon Animator View, тому тут ми їх
        // просто не чіпаємо - інакше локальний Update() чужої копії
        // перезаписував би щойно отримані з мережі значення.
        if (!photonView.IsMine) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float speed = new Vector2(horizontal, vertical).magnitude;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && speed > moveThreshold;
        if (isSprinting)
            speed *= sprintMultiplier;

        bool isMoving = speed > moveThreshold;
        handAnimator.SetBool("IsMoving", isMoving);
        handAnimator.SetFloat("speed", speed);

        if (Input.GetKeyDown(jumpKey) && !isJumping && isGrounded)
        {
            isJumping = true;
            handAnimator.SetBool("isJumping", isJumping);
            handAnimator.SetTrigger("jump");
        }
    }

    // Ці публічні методи викликає PlayerPickup - але PlayerPickup сам вже
    // вимкнений на чужих копіях (photonView.IsMine у PlayerPickup), тож
    // сюди теж ніколи не прийде виклик з чужого клієнта.
    public void SetHolding(bool holding)
    {
        isHolding = holding;
        handAnimator.SetBool("IsHolding", isHolding);
    }

    public void SetGrounded(bool grounded)
    {
        bool wasGrounded = isGrounded;
        isGrounded = grounded;
        handAnimator.SetBool("isGrounded", isGrounded);

        if (isGrounded && !wasGrounded)
            SetJumpFinished();
    }

    public void SetJumpFinished()
    {
        isJumping = false;
        handAnimator.SetBool("isJumping", isJumping);
    }

    public void PlayEquipAnimation(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) return;
        handAnimator.SetTrigger(triggerName);
    }

    public void SetFishingEquipped(bool equipped)
    {
        isFishingEquipped = equipped;
        handAnimator.SetBool("IsFishingEquipped", isFishingEquipped);
    }
}
