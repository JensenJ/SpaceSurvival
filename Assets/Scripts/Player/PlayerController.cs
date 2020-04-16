using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

//The player controller is a unit controlled by a player

[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(PlayerSetup))]
[RequireComponent(typeof(PlayerFlashLight))]
[RequireComponent(typeof(PlayerAttributes))]
public class PlayerController : NetworkBehaviour
{
    [SyncVar(hook = nameof(HookSetActiveState))] public bool activeState = true;
    public bool isCursorEnabled = false;
    public bool canMove = true;
    [SerializeField] [Range(0, 10)] float speed = 5.0f;
    [SerializeField] [Range(0, 15)] float sprintSpeed = 7.0f;
    [SerializeField] [Range(0, 5)] public float sensitivity = 3.0f;
    [SerializeField] [Range(0, 5)] public float jumpForce = 2.0f;

    [SerializeField] [Range(0, 10)] float baseSpeed = 5.0f;
    [SerializeField] [Range(0, 15)] float baseSprintSpeed = 7.0f;

    [Header("Attributes:")]
    [SerializeField] [Range(0.0f, 10.0f)] public float staminaDrainSpeed = 2.5f;
    [SerializeField] [Range(0.0f, 7.0f)] public float staminaRegenSpeed = 1.25f;

    GameObject gameManager = null;
    PlayerMotor motor = null;
    PlayerFlashLight flashLight = null;
    PlayerAttributes attributes = null;

    // Start is called before the first frame update
    void Start()
    {
        //Start settings
        motor = GetComponent<PlayerMotor>();
        flashLight = GetComponent<PlayerFlashLight>();
        attributes = GetComponent<PlayerAttributes>();
        gameManager = GameObject.FindGameObjectWithTag("GameController");
        DisableCursor();
    }

    // Update is called once per frame
    void Update()
    {
        //Check that this is owned by player
        if (hasAuthority == false)
        {
            return;
        }

        //Toggle cursor activation
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            if (isCursorEnabled)
            {
                DisableCursor();
                canMove = true;
            }
            else
            {
                EnableCursor();
                canMove = false;
            }
        }

        //Activate flash light
        if (Input.GetKeyDown(KeyCode.F))
        {
            flashLight.ToggleFlashLight(!flashLight.GetFlashLightStatus());
        }

        //Movement
        Move();
        Rotate();
        Jump();
    }

    //Function to enable cursor, used in menus
    public void EnableCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        isCursorEnabled = true;
    }

    //Function to disable cursor, used in menus
    public void DisableCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        isCursorEnabled = false;
    }

    //Movement
    void Move()
    {
        Vector3 velocity = Vector3.zero;
        float moveSpeed = speed;
        //Checks whether player can move
        if (canMove)
        {
            //If sprinting and stamina is above 10%
            if (Input.GetButton("Sprint"))
            {
                attributes.DamageStamina(staminaDrainSpeed * Time.deltaTime);
                if (attributes.GetStamina() / attributes.GetMaxStamina() >= 0.1f)
                {
                    moveSpeed = sprintSpeed;
                }
            }
            else
            {
                if (attributes.GetStamina() < attributes.GetMaxStamina())
                {
                    attributes.HealStamina(staminaRegenSpeed * Time.deltaTime);
                }
            }

            float xMove = Input.GetAxisRaw("Horizontal");
            float zMove = Input.GetAxisRaw("Vertical");

            Vector3 moveHorizontal = transform.right * xMove;
            Vector3 moveVertical = transform.forward * zMove;

            velocity = (moveHorizontal + moveVertical).normalized * moveSpeed;
        }

        motor.Move(velocity);
    }

    //Rotation
    void Rotate()
    {
        Vector3 rotation = Vector3.zero;
        float CameraRotationX = 0.0f;
        //Checks whether player can move
        if (canMove)
        {
            float yRot = Input.GetAxisRaw("Mouse X");
            float xRot = Input.GetAxisRaw("Mouse Y");

            rotation = new Vector3(0.0f, yRot, 0.0f) * sensitivity;
            CameraRotationX = xRot * sensitivity;
        }
        motor.Rotate(rotation);
        motor.RotateCamera(CameraRotationX);
    }

    void Jump()
    {
        if (canMove)
        {
            if (Input.GetButtonDown("Jump") && motor.IsGrounded())
            {
                motor.Jump(jumpForce);
            }
        }
    }

    //SETTERS
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetSprintSpeed(float newSprintSpeed)
    {
        sprintSpeed = newSprintSpeed;
    }


    //GETTERS
    public float GetSpeed()
    {
        return speed;
    }
    public float GetSprintSpeed()
    {
        return sprintSpeed;
    }

    public float GetBaseSpeed()
    {
        return baseSpeed;
    }

    public float GetBaseSprintSpeed()
    {
        return baseSprintSpeed;
    }

    //Hook function to set the active state for syncvar
    public void HookSetActiveState(bool oldState, bool newState)
    {
        activeState = newState;
        gameObject.SetActive(newState);
    }

    //Command to set the active state
    [Command]
    public void CmdChangeActiveState(bool newState)
    {
        activeState = newState;
        gameObject.SetActive(newState);
    }
}