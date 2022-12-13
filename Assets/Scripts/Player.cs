using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;

    private Transform camera;
    private World world;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float playerHeight = 1.8f; // some avatars may need to change this value
    public float gravity = -5f;     // should gravity be a private static value?
    public float jumpForce = 5f;
    public int mouseSensitivity = 25;

    public float playerWidth = 0.15f;   // using capsule collider so we dont need height

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private float verticalMomentum = 0;
    private bool jumpRequest;
    private Vector3 velocity;

    public Transform highlightBlock;
    public Transform placeBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Text selectedBlockText;
    // blocks you want to place
    public byte selectedBlockIndex = 1;

    private void Start()
    {
        camera = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        Cursor.lockState = CursorLockMode.Locked;
        selectedBlockText.text = world.blockTypes[selectedBlockIndex].blockName;
    }

    private void FixedUpdate()
    {
        GetPlayerInput();

        CalculateVelocity();

        if (jumpRequest)
            Jump();

        // Rotate the entire character, not just the mouse
        transform.Rotate(Vector3.up * mouseHorizontal * mouseSensitivity);

        // If we support mouse inversion, we change the -+ values here
        camera.Rotate(Vector3.right * -mouseVertical * mouseSensitivity);

        // move player relative to the world
        transform.Translate(velocity, Space.World);
    }

    private void Update()
    {
        GetPlayerInput();
        PlaceCursorBlocks();
    }

    private void Jump ()
    {
        verticalMomentum = jumpForce;

        isGrounded = false;
        jumpRequest = false;
    }

    // Handle Player movement
    private void CalculateVelocity()
    {
        // Affect vertical momentum with gravity
        if (verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;

        // Check if player is walking / sprinting
        if (isSprinting)
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        else
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;

        // Check if player if falling / jumping
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        // Can player move where they are trying to move?
        // Player is moving on x,z axis, is something in the way
        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
            velocity.z = 0;
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
            velocity.x = 0;

        // Player is moving on y axis, is something in the way
        if (velocity.y < 0)
            velocity.y = CheckDownSpeed(velocity.y);
        else if (velocity.y > 0)
            velocity.y = CheckUpSpeed(velocity.y);
    }

    private void GetPlayerInput()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        // Sprint is set in Project Settings to be left-shift. This allows players to remap controls in future.
        if (Input.GetButtonDown("Sprint"))
        {
            isSprinting = true;
        }
        if (Input.GetButtonUp("Sprint"))
        {
            isSprinting = false;
        }

        /// TODO: We want double/triple jumps. Will need refactoring.
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            jumpRequest = true;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0)
            {
                selectedBlockIndex++;
            }
            else
            {
                selectedBlockIndex--;
            }

            if (selectedBlockIndex > (byte)(world.blockTypes.Length - 1))
            {
                selectedBlockIndex = 1;
            }

            if (selectedBlockIndex < 1)
            {
                selectedBlockIndex = (byte)(world.blockTypes.Length - 1);
            }

            selectedBlockText.text = world.blockTypes[selectedBlockIndex].blockName;
        }

        if (highlightBlock.gameObject.activeSelf)
        {
            // Left mouse click
            if(Input.GetMouseButtonDown(0))
            {
                //Destroy Block
                world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
            }
            // Right mouse click
            if (Input.GetMouseButtonDown(1))
            {
                //Place Block
                world.GetChunkFromVector3(highlightBlock.position).EditVoxel(placeBlock.position, selectedBlockIndex);
            }
        }
    }

    // Faking a raycast. Is this the best idea?
    private void PlaceCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 pos = camera.position + (camera.forward * step);

            if (world.CheckForVoxel(pos))
            {
                highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;

                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            step += checkIncrement;
        }

        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);

    }

    // Determines if there is a solid block below the player. If yes, then player doesnt fall. If no, player falls to next solid block.
    // Also modifies isGrounded to allow or deny the player their jump ability
    private float CheckDownSpeed(float downSpeed)
    {
        // Check all 4 corners of player model. If any of them return true, then there is a solid voxel beneath player (something to stand on)
        if ((world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) && (!left && !back)) ||
            (world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) && (!right && !back)) ||
            (world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) && (!right && !front)) ||
            (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))) && (!left && !front))
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    // Determines if there is a solid block above the player. If yes, then player can move up.
    private float CheckUpSpeed(float upSpeed)
    {
        // Check all 4 corners of player model. If any of them return true, then there is a solid voxel beneath player (something to stand on)
        if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + playerHeight + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + playerHeight + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + playerHeight, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + playerHeight, transform.position.z + playerWidth)))
        {
            verticalMomentum = 0;
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    // Player is two blocks tall. Check the player's "feet" block and player's "head" block. If neither of them are blocked by an obstacle, allow movement.
    public bool front
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // Player is two blocks tall. Check the player's "feet" block and player's "head" block. If neither of them are blocked by an obstacle, allow movement.
    public bool back
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // Player is two blocks tall. Check the player's "feet" block and player's "head" block. If neither of them are blocked by an obstacle, allow movement.
    public bool left
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // Player is two blocks tall. Check the player's "feet" block and player's "head" block. If neither of them are blocked by an obstacle, allow movement.
    public bool right
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
