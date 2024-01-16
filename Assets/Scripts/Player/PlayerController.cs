using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private Rigidbody rb;
    public Camera cam;

    public bool isGrounded;
    public float groundCheckDistance = 0.2f;

    public float sensitivity = 2.0f;
    private float verticalRotation = 0f;
    private float verticalRotationLimit = -80f;
    public bool canMove = true;


    // Start is called before the first frame update
    void Start()
    {
        //TODO: Move this if there is a global script.
        // Setting frame rate
#if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;     // Disable VSync
        Application.targetFrameRate = 60;   // Set frame rate
#endif

        // Lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //////

        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        if (canMove)
        {
            isGrounded = Physics.Raycast(rb.transform.position, Vector3.down, groundCheckDistance);

            // Movement
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            Vector3 movement = (transform.forward * verticalInput + transform.right * horizontalInput) * moveSpeed;
            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);


            // Jumping
            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }

            // Mouse rotation for the player left-right movement.
            float horizontalRotationInput = Input.GetAxis("Mouse X") * sensitivity;
            transform.Rotate(Vector3.up * horizontalRotationInput);

            // Mouse rotation for the camera up-down movement with limits.
            float mouseY = Input.GetAxis("Mouse Y");
            if (mouseY != 0)
            {
                float verticalRotationInput = -mouseY * sensitivity;
                verticalRotation += verticalRotationInput;
                verticalRotation = Mathf.Clamp(verticalRotation, verticalRotationLimit, -verticalRotationLimit);
                cam.transform.localEulerAngles = new Vector3(verticalRotation, 0, 0);
            }
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }
}
