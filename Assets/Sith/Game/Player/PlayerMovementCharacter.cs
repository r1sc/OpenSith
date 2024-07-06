using UnityEngine;
using System.Collections;

// This script moves the character controller forward
// and sideways based on the arrow keys.
// It also jumps when pressing space.
// Make sure to attach a character controller to the same game object.
// It is recommended that you make only one call to Move or SimpleMove per frame.

public class PlayerMovementCharacter : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    public float AirDrag = 0.5f;
    public float MaxThrust = 2.0f;
    public float ExtraSpeed = 0.0f;
    public float JumpSpeed = 1.2f;
    public float Gravity = -9.81f;
    public float SurfaceDrag = 0.3f;

    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
    }

    void ApplyDrag(ref Vector3 vel, float drag, float mag, float msDeltaTime)
    {
        if(mag == 0.0f || vel.magnitude >= mag)
        {
            if (drag != 0.0f)
            {
                var ddrag = drag * msDeltaTime;
                if (ddrag > 1.0f)
                    ddrag = 1.0f;

                vel += vel * -ddrag;
                if ((vel.x < 0.0 ? -vel.x : vel.x) < 0.0000099999997f)
                   vel.x = 0.0f;

                if ((vel.y < 0.0 ? -vel.y : vel.y) < 0.0000099999997f)
                    vel.y = 0.0f;

                if ((vel.z < 0.0 ? -vel.z : vel.z) < 0.0000099999997f)
                    vel.z = 0.0f;
            }
        }
        else
        {
            vel = Vector3.zero;
        }
    }

    void DoPlayerInAirPhysics()
    {

    }

    void FixedUpdate()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = transform.TransformVector(new (Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
        var speed = Input.GetButton("Run") ? MaxThrust * 1.5f : MaxThrust;
        playerVelocity = speed * Time.deltaTime * move;

        //if (move != Vector3.zero)
        //{
        //    gameObject.transform.forward = move;
        //}

        // Changes the height position of the player..
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y += JumpSpeed;///Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        groundedPlayer = (Vector3.Angle(Vector3.up, hitNormal) <= controller.slopeLimit);
        if (!groundedPlayer)
        {
            playerVelocity.x = (1f - hitNormal.y) * hitNormal.x * (1f - SurfaceDrag);
            playerVelocity.z = (1f - hitNormal.y) * hitNormal.z * (1f - SurfaceDrag);
        }

        playerVelocity.y += Gravity * Time.deltaTime;
        //ApplyDrag(ref playerVelocity, AirDrag, 0.0f, Time.deltaTime);
        controller.Move(playerVelocity);
    }

    Vector3 hitNormal;
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        hitNormal = hit.normal;
    }
}