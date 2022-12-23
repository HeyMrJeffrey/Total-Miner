using System.Collections;
using System.Collections.Generic;
using TotalMinerUnity;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;

    public Transform cameraTransform;
    private World world;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float playerHeight = 1.8f; // some avatars may need to change this value
    public float gravity = -5f;     // should gravity be a private static value?
    public float jumpForce = 5f;

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

    public Toolbar toolbar;


    private void Start()
    {
        cameraTransform = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        world.inUI = false;
    }

    private void FixedUpdate()
    {
        //GetPlayerInput();
        //Removed this for the inventory system to prvent the player from moving while looking at their inventory.
        //Do we want player input on update or fixed upate?

        if (!world.inUI)
        { 
            CalculateVelocity();

            if (jumpRequest)
                Jump();

            // Rotate the entire character, not just the mouse
            transform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivity);

            // If we support mouse inversion, we change the -+ values here
            cameraTransform.Rotate(Vector3.right * -mouseVertical * world.settings.mouseSensitivity);

            // move player relative to the world
            transform.Translate(velocity, Space.World);
        }
    }

    private void Update()
    {
        /// TODO: make this button press programmable instead of hard codingto 'I'
        if (Input.GetKeyDown(KeyCode.I))
        {
            world.inInventory = !world.inInventory;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!world.inInventory)
            {
                NoesisView nv = GameObject.Find("Main Camera").GetComponent<NoesisView>();

                

                world.inPauseMenu = !world.inPauseMenu;
                if (world.inPauseMenu)
                {
                    nv.enabled = true;

                }
                else
                {
                    //var j = new SubMenuTest();
                    //
                    //nv.Xaml.uri = "SubMenuTest";
                    //nv.LoadXaml(false);
                    nv.enabled = false;

                }
            }
            else
                world.inInventory = false;
        }

        if (!world.inUI)
        {
            GetPlayerInput();
            PlaceCursorBlocks();
        }

        
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
        // Specifically for testing a build
        //if (Input.GetKeyDown(KeyCode.Escape))
        //{
        //    Application.Quit();
        //}

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
                // If there is an item selected
                if (toolbar.slots[toolbar.slotIndex].HasItem)
                {
                    //Place Block
                    world.GetChunkFromVector3(placeBlock.position).EditVoxel(placeBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                    toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
                }
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
            Vector3 pos = cameraTransform.position + (cameraTransform.forward * step);

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
