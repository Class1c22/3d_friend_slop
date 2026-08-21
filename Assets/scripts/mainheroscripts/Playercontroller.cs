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

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetButtonDown("Jump");

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f && cameraTransform != null)
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

        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateAnimator(inputDir.magnitude, isRunning, jumpPressed);
    }

    void UpdateAnimator(float moveMagnitude, bool isRunning, bool jumpPressed)
    {
        if (animator == null) return;

        float speedValue = moveMagnitude * (isRunning ? runSpeed : walkSpeed);
        animator.SetFloat("speed", speedValue);
        animator.SetBool("isGrounded", isGrounded);

        if (jumpPressed && isGrounded)
            animator.SetTrigger("Jump");
    }
}