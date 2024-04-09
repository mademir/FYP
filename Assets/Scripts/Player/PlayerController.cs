using System;
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
    public bool canMove = false;
    bool walking = false;

    public GameController gameController;
    public AudioSource audioSource;

    public void LockCursor(bool state)
    {
        Cursor.lockState = state? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !state;
    }

    // Start is called before the first frame update
    void Start()
    {
        //TODO: Move this if there is a global script.
        // Setting frame rate
#if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;     // Disable VSync
        Application.targetFrameRate = 60;   // Set frame rate
#endif

        //////


        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) gameController.ToggleEscapeMenu();
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

            if (Math.Abs(rb.velocity.x) > 0.01f || Math.Abs(rb.velocity.z) > 0.01f)
            {
                if (!walking) GetComponent<NetworkNode>().PlayAudio("", gameController.client, true);
                walking = true;
            }
            else
            {
                if (walking) GetComponent<NetworkNode>().PauseAudio(gameController.client, true);
                walking = false;
            }

            // Jumping
            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                audioSource.GetComponent<NetworkNode>().PlayAudio("Audio/jump", gameController.client, true);
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

    public void Die()
    {
        transform.SetPositionAndRotation(gameController.CurrentSpawn.position, gameController.CurrentSpawn.rotation);
        audioSource.GetComponent<NetworkNode>().PlayAudio("Audio/death", gameController.client, true);
    }
}
