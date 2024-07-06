using Assets.Sith.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MoveSlideCapsule))]
public class MovePlayer : MonoBehaviour
{

    public float Friction     = 0.1f;
    public float Acceleration = 5;
    public float MaxVelocity  = 5;
    public float JumpVelocity = 5;

    private MoveSlideCapsule _moveSlide;
    private Camera _camera;

    // Use this for initialization
    void Start()
    {
        _moveSlide = GetComponent<MoveSlideCapsule>();
        _camera = GetComponentInChildren<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_moveSlide.IsGrounded)
        {
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");

            if (horizontal == 0 && vertical == 0)
            {
                _moveSlide.Velocity *= Mathf.Pow(Friction, Time.deltaTime);
            }
            else
            {
                var acceleration = Acceleration * horizontal * Vector3.right + Acceleration * vertical * Vector3.forward;
                var newVelocity  = _moveSlide.Velocity + acceleration * Time.deltaTime;

                var maxVelocity     = Input.GetButton("Run") ? MaxVelocity * 2.0f : MaxVelocity;
                var realVelY        = newVelocity.y;
                newVelocity.y       = 0;
                newVelocity         = Vector3.ClampMagnitude(newVelocity, maxVelocity);
                newVelocity.y       = realVelY;
                _moveSlide.Velocity = newVelocity;
            }
        }

        if (Input.GetButton("Jump"))
        {
            _moveSlide.Velocity += Vector3.up * JumpVelocity;
        }
        else if (Input.GetButton("Crouch"))
        {
            _moveSlide.Velocity += Vector3.down * JumpVelocity;
        }
    }
}
