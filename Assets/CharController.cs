using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharController : MonoBehaviour
{
    public float Acceleration = 5; // M/s
    public float Friction = 0.9f;
    public float SlopeSlideSpeed = 5;
    public float MaxVelocity = 5;

    public float MouseSensitivity = 1.0f;
    public float JumpStrength = 2;

    private Transform _cameraPivot;
    private Vector3 _velocity;
    private CharacterController _charController;
    private Vector3 _worldVelocity;

    // Use this for initialization
    void Start()
    {
        _cameraPivot = transform.Find("CameraPivot");
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        _charController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCameraRotation();
    }

    void FixedUpdate()
    {
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        var forwardMovement = 0;
        if (Input.GetKey(KeyCode.W))
            forwardMovement = 1;
        else if (Input.GetKey(KeyCode.S))
            forwardMovement = -1;

        var strafeMovement = 0;
        if (Input.GetKey(KeyCode.D))
            strafeMovement = 1;
        else if (Input.GetKey(KeyCode.A))
            strafeMovement = -1;

        if (forwardMovement == 0 && strafeMovement == 0)
            _velocity *= Mathf.Pow(Friction, Time.deltaTime);
        else
        {
            _velocity.x += strafeMovement * Acceleration * Time.deltaTime;
            _velocity.z += forwardMovement * Acceleration * Time.deltaTime;
        }

        _velocity = Vector3.ClampMagnitude(_velocity, MaxVelocity);

        var moveVelocity = _worldVelocity + transform.forward * _velocity.z + transform.right * _velocity.x;

        var flags = _charController.Move(moveVelocity * Time.deltaTime);
        if ((flags & CollisionFlags.Below) != 0)
        {
            if (_worldVelocity.y < 0)
            {
                _worldVelocity.y = 0;
            }
        }
        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, _charController.height / 2 + 0.1f))
        {
            //var slopeAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
            //if (slopeAngle >= _charController.slopeLimit)
            //{
            var newVel = hitInfo.normal * SlopeSlideSpeed * Time.deltaTime;

            Debug.Log(" new vel: " + newVel);
            _worldVelocity += newVel;

            if (Input.GetKey(KeyCode.Space))
            {
                _worldVelocity.y += JumpStrength;
            }
        }

        _worldVelocity.x *= Mathf.Pow(Friction, Time.deltaTime);
        _worldVelocity.z *= Mathf.Pow(Friction, Time.deltaTime);
        _worldVelocity.y += Physics.gravity.y * Time.deltaTime;

        //RaycastHit hitInfo;
        //if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, 2))
        //{
        //    worldVelocity.y = 0;
        //    Debug.Log("Floor");
        //    transform.position = hitInfo.point + hitInfo.normal * 2;
        //}

        //_rigidbody.velocity = worldVelocity;


        //if (Input.GetKeyDown(KeyCode.Space))
        //    _rigidbody.AddForce(Vector3.up * JumpStrength, ForceMode.Acceleration);
    }

    private void UpdateCameraRotation()
    {
        var mx = Input.GetAxis("Mouse X") * MouseSensitivity;
        var my = Input.GetAxis("Mouse Y") * MouseSensitivity;

        var camPivEuler = _cameraPivot.localRotation.eulerAngles;
        camPivEuler.x += -my * Time.deltaTime;
        _cameraPivot.localRotation = Quaternion.Euler(camPivEuler);

        var euler = transform.rotation.eulerAngles;
        euler.y += mx * Time.deltaTime;
        transform.rotation = Quaternion.Euler(euler);
    }
}
