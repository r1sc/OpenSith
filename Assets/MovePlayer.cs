using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MoveSlideCapsule))]
public class MovePlayer : MonoBehaviour {

    public float Friction = 0.1f;
    public float Acceleration = 5;
    public float MaxVelocity = 5;
    public float JumpVelocity = 5;

    private MoveSlideCapsule _moveSlide;


    // Use this for initialization
	void Start ()
	{
	    _moveSlide = GetComponent<MoveSlideCapsule>();
	}
	
	// Update is called once per frame
	void Update ()
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
	            var acceleration = horizontal * Vector3.right * Acceleration + vertical * Vector3.forward * Acceleration;
	            var newVelocity = _moveSlide.Velocity + acceleration * Time.deltaTime;

	            var realVelY = newVelocity.y;
	            newVelocity.y = 0;
	            newVelocity = Vector3.ClampMagnitude(newVelocity, MaxVelocity);
	            newVelocity.y = realVelY;
	            _moveSlide.Velocity = newVelocity;
	        }

	        if (Input.GetButton("Jump"))
	        {
	            _moveSlide.Velocity += Vector3.up * JumpVelocity;
	        }
	    }
	}
}
