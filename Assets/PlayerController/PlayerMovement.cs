using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour {
    private Camera _camera;
    public AudioSource WalkSoundSource;

    // Use this for initialization
	void Start ()
	{
	    _camera = GetComponentInChildren<Camera>();
	    _rigidBody = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
    private float an = 0;
    public int MaxSpeed = 2;
    public float Acceleration = 1;
    private float _lastWalkPlayTime = 0;
    private Rigidbody _rigidBody;

    void FixedUpdate ()
    {
        var realMaxSpeed = MaxSpeed;
        if (Input.GetButton("Run"))
            realMaxSpeed *= 2;
        var forward = Input.GetAxis("Vertical");
        var strafe = Input.GetAxis("Horizontal");
        var newVel = Vector3.ClampMagnitude((transform.forward * forward) + (transform.right * strafe), 1) * realMaxSpeed;

        an += newVel.magnitude * 2;
        an = an % 360;
        //if (Mathf.Abs(newVel.magnitude) > (MaxSpeed/2.0f))
        //{
        //}
        //else
        //{
        //    newVel *= 0.9f;
        //}
        if (newVel.magnitude > 0.1f)
        {
            if (Time.time > _lastWalkPlayTime + Mathf.Pow(0.9f, newVel.magnitude)) {
                _lastWalkPlayTime = Time.time;
                WalkSoundSource.pitch = Random.Range(0.9f, 1.5f);
                WalkSoundSource.Play();
            }
        }
        newVel.y = _rigidBody.velocity.y;
        _rigidBody.velocity = newVel;

        _camera.transform.localPosition = new Vector3(0, Mathf.Sin(an * Mathf.Deg2Rad) / 20.0f, 0);
	}
}
