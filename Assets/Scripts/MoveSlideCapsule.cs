using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSlideCapsule : MonoBehaviour
{
    private CapsuleCollider _collider;
    private Vector3 _worldVelocity;

    public float LegHeight = 0.5f;
    public float SteepestSlope = 20;
    public Vector3 Velocity;
    public bool IsGrounded;

    // Use this for initialization
    void Start()
    {
        _collider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsGrounded)
            _worldVelocity = transform.TransformVector(Velocity);
        _worldVelocity += Physics.gravity * Time.deltaTime;

        var newPos = transform.position + _worldVelocity * Time.deltaTime;

        var halfHeight = _collider.height / 2;
        var wasGrounded = IsGrounded;
        IsGrounded = false;

        if (Velocity.y <= 0)
        {
            var rayLength = Mathf.Max(halfHeight + LegHeight * 2, _worldVelocity.y);
            RaycastHit hitInfo;
            if (Physics.Raycast(newPos, Vector3.down, out hitInfo, rayLength))
            {
                if (wasGrounded || hitInfo.distance <= halfHeight + LegHeight)
                {
                    newPos.y = hitInfo.point.y + halfHeight + LegHeight;

                    var angle = Vector3.Angle(Vector3.up, hitInfo.normal);
                    // Debug.Log("Surface angle = " + angle);
                    if (angle < SteepestSlope)
                    {
                        IsGrounded = true;
                        _worldVelocity.y = 0;
                    }
                    else
                    {
                        //newPos += hitInfo.normal * Mathf.Max(0, 0.5f - hitInfo.distance);
                        _worldVelocity -= hitInfo.normal * Vector3.Dot(_worldVelocity, hitInfo.normal);
                    }
                }
            }
        }

        var heightWithSphere = halfHeight - _collider.radius * 2;
        var capsuleTop = newPos + Vector3.up * heightWithSphere;
        var capsuleBottom = newPos + Vector3.down * heightWithSphere;

        var velocityDirection = Vector3.zero;

        while (true)
        {
            var colliders = Physics.OverlapCapsule(capsuleBottom, capsuleTop, _collider.radius, 1);
            var hit = false;
            foreach (var hitCollider in colliders)
            {
                Vector3 direction;
                float distance;

                if (Physics.ComputePenetration(_collider, newPos, Quaternion.identity, hitCollider,
                    hitCollider.transform.position, hitCollider.transform.rotation, out direction, out distance))
                {
                    velocityDirection += direction;
                    newPos += direction * distance;
                    hit = true;
                }
            }
            if(!hit)
                break;
        }

        var velocityDisplacement =
            velocityDirection.normalized * Vector3.Dot(_worldVelocity, velocityDirection.normalized);
        velocityDisplacement.y = 0;
        _worldVelocity -= velocityDisplacement;
    

        if (Mathf.Abs(_worldVelocity.x) < 0.1)
            _worldVelocity.x = 0;
        if (Mathf.Abs(_worldVelocity.z) < 0.1)
            _worldVelocity.z = 0;
        
        transform.position = newPos;

        Velocity = transform.InverseTransformVector(_worldVelocity);
    }
}
