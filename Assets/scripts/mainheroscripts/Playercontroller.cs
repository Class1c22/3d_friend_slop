using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Рух")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Стрибок / Гравітація")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Посилання")]
    public Animator animator;
    public Transform cameraTransform; // перетягни сюди Main Camera

    [Tooltip("Скрипт рук, який теж треба сповіщати про приземлення і землю, щоб jump можна було повторити")]
    public HandAnimatorController handAnimatorController;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        if (handAnimatorController == null)
            handAnimatorController = GetComponentInChildren<HandAnimatorController>();
    }

    void Update()
    {
        bool wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        // Сповіщаємо скрипт рук про землю КОЖЕН кадр, коли стан змінився -
        // так isGrounded в руках завжди синхронний з isGrounded персонажа
        if (isGrounded != wasGrounded && handAnimatorController != null)
            handAnimatorController.SetGrounded(isGrounded);

        // Щойно приземлились - скидаємо стан стрибка і "прибиваємо" до землі
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

        // Стрибок можна почати лише стоячи на землі і якщо ще не в стрибку -
        // це не дає натисканню кнопки в повітрі повторно тригерити анімацію
        bool jumpPressed = Input.GetButtonDown("Jump") && isGrounded && !isJumping;

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        bool isMoving = inputDir.magnitude >= 0.1f;

        if (isMoving && cameraTransform != null)
        {
            float speed = isRunning ? runSpeed : walkSpeed;

            // Напрямок руху рахуємо від камери (без Y компоненти), а не від тіла
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

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateAnimator(inputDir.magnitude, isMoving && isRunning, jumpPressed);
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
    }
}