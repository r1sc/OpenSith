using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharController : MonoBehaviour
{
    public float Acceleration = 5; // M/s
    public float Friction = 0.9f;
    public float MaxVelocity = 5;

    public float MouseSensitivity = 1.0f;
    public float JumpStrength = 2;

    private Transform _cameraPivot;
    private Rigidbody _rigidbody;
    private Vector3 _velocity;

    // Use this for initialization
    void Start()
    {
        _cameraPivot = transform.Find("CameraPivot");
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        _rigidbody = GetComponent<Rigidbody>();
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

        var worldVelocity = (transform.forward * _velocity.z + transform.right * _velocity.x);
        _rigidbody.velocity = worldVelocity;


        if (Input.GetKeyDown(KeyCode.Space))
            _rigidbody.AddForce(Vector3.up * JumpStrength, ForceMode.Acceleration);
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
