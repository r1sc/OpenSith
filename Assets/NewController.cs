using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewController : MonoBehaviour
{
    public Vector3 Velocity;
    private RaycastHit hitInfo;
    public float FallingVel;

    RaycastHit sphereInfo;

    // Update is called once per frame
    void Update()
    {
        Velocity.y = 0;
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");

        if (vertical != 0)
        {
            Velocity += transform.forward * vertical * 5 * Time.deltaTime;
        }
        if (horizontal != 0)
        {
            Velocity += transform.right * horizontal * 5 * Time.deltaTime;
        }

        if (horizontal == 0 && vertical == 0)
            Velocity *= Mathf.Pow(0.1f, Time.deltaTime);

        FallingVel += Physics.gravity.y * Time.deltaTime;

        Velocity = Vector3.ClampMagnitude(Velocity, 5);


        var newPos = transform.position + (Velocity + Vector3.up * FallingVel) * Time.deltaTime;

        if (Physics.Raycast(newPos, Vector3.down, out hitInfo, 1))
        {
            newPos += hitInfo.normal * (1 - hitInfo.distance);
            FallingVel = 0;
        }


        for (var i = 0; i < 3; i++)
        {
            var xzVelocity = Velocity;
            xzVelocity.y = 0;
            if (Physics.SphereCast(newPos, 0.5f, xzVelocity.normalized, out sphereInfo))
            {
                if (sphereInfo.distance < 0.5f)
                {
                    newPos += sphereInfo.normal * (0.5f - sphereInfo.distance);
                    Velocity -= sphereInfo.normal * Vector3.Dot(Velocity, sphereInfo.normal);
                    //Velocity.y = 0;
                }
            }
        }
        transform.position = newPos;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(hitInfo.point, 0.5f);
        Gizmos.DrawWireSphere(sphereInfo.point, 0.5f);
    }
}
