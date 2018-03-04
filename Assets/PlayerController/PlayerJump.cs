using UnityEngine;
using System.Collections;

public class PlayerJump : MonoBehaviour {
    private Rigidbody _rigidbody;
    private CapsuleCollider _capsuleCollider;
    public float JumpStrength = 7;

    // Use this for initialization
	void Start () {
	    _rigidbody = GetComponent<Rigidbody>();
	    _capsuleCollider = GetComponent<CapsuleCollider>();
	}

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, _capsuleCollider.height / 2.0f + 0.1f))
            {
                if (_rigidbody.velocity.y < 0.1f)
                    _rigidbody.velocity += Vector3.up * JumpStrength;
            }
        }
    }
}
