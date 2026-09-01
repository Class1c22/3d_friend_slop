using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviourPun
{
    [Header("Рух")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Стрибок / Гравітація")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Підводна гравітація")]
    [Tooltip("Множник до gravity, коли гравець під водою (0.3 = 30% від звичайної)")]
    public float underwaterGravityMultiplier = 0.3f;

    [Header("Посилання")]
    public Animator animator;
    public Transform cameraTransform; // перетягни сюди Main Camera

    [Tooltip("Скрипт рук, який теж треба сповіщати про приземлення і землю, щоб jump можна було повторити")]
    public HandAnimatorController handAnimatorController;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;
    private bool isUnderwater;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        if (handAnimatorController == null)
            handAnimatorController = GetComponentInChildren<HandAnimatorController>();

        // ВАЖЛИВО: якщо цей об'єкт заспавнений PhotonNetwork.Instantiate і належить
        // ІНШОМУ гравцю - вимикаємо скрипт повністю. Без цього Update() читав би
        // ЛОКАЛЬНИЙ Input цього клієнта і рухав би чужого персонажа.
        // (PlayerRig.cs теж це робить при спавні, ця перевірка - підстраховка
        // на випадок, якщо скрипт увімкнули вручну або PlayerRig не призначений.)
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        bool wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        if (isGrounded != wasGrounded && handAnimatorController != null)
            handAnimatorController.SetGrounded(isGrounded);

        if (isGrounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

            if (isJumping)
                isJumping = false;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        bool jumpPressed = Input.GetButtonDown("Jump") && isGrounded && !isJumping;

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        bool isMoving = inputDir.magnitude >= 0.1f;

        if (isMoving && cameraTransform != null)
        {
            float speed = isRunning ? runSpeed : walkSpeed;

            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDir = camRight * inputDir.x + camForward * inputDir.z;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        if (jumpPressed)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
        }

        float currentGravity = isUnderwater ? gravity * underwaterGravityMultiplier : gravity;
        velocity.y += currentGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateAnimator(inputDir.magnitude, isMoving && isRunning, jumpPressed);
    }

    /// <summary>
    /// Викликається PlayerBreath при вході/виході з води - вмикає/вимикає
    /// зменшену гравітацію на час перебування під водою.
    /// </summary>
    public void SetUnderwater(bool value)
    {
        isUnderwater = value;
    }

    void UpdateAnimator(float moveMagnitude, bool isSprinting, bool jumpPressed)
    {
        if (animator == null) return;

        float speedValue = moveMagnitude * (isSprinting ? runSpeed : walkSpeed);
        animator.SetFloat("speed", speedValue);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isRunning", isSprinting);
        animator.SetBool("isJumping", isJumping);

        if (jumpPressed)
            animator.SetTrigger("jump");

        // Ці параметри Animator читає компонент Photon Animator View (додати
        // в інспекторі на PhotonView гравця -> Observed Components) і сам
        // розсилає їх іншим клієнтам - додаткового коду для цього не треба.
    }
}