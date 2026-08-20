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
    public Animator animator; // перетягніть сюди Animator з мешу персонажа

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // невеликий притиск до землі

        // Ввід руху
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetButtonDown("Jump");

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            float speed = isRunning ? runSpeed : walkSpeed;

            // Персонаж рухається відносно напрямку камери, але САМ НІКОЛИ НЕ РОЗВЕРТАЄТЬСЯ.
            // W - вперед, S - назад (спиною), A/D - боком. Орієнтація тіла не змінюється рухом.
            Quaternion camFacing = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            Vector3 moveDir = camFacing * inputDir;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        // Стрибок
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Гравітація
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Керування анімаціями
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