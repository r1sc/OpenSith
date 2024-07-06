using System.Collections;
using UnityEngine;

namespace Assets.Sith.Game.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        public float Speed      = 10.0f;
        public float JumpSpeed  = 6.0f;
        public float MaxSlope   = 45f;
       // public float StepHeight = 0.41f;
        public float StepSmooth = 20f;

        private Rigidbody _rb;
        private float _groundDistance = 0.0f;
        private bool _isGrounded      = false;
        private CapsuleCollider _collider;

        private void Start()
        {
            _groundDistance = GetComponent<Collider>().bounds.extents.y;
            _collider       = GetComponent<CapsuleCollider>();
            _rb             = GetComponent<Rigidbody>();
        }

        //private bool IsGrounded()
        //{
        //    return Physics.Raycast(transform.position, Vector3.down, GroundDistance);
        //}

        //void Update()
        //{
        //    //Movement = new Vector3(Input.GetAxis("Horizontal"), PlayerBody.velocity.y, Input.GetAxis("Vertical"));

        //    //Vector3 move = transform.TransformDirection(Movement) * Speed;
        //    //PlayerBody.velocity = new Vector3(move.x, PlayerBody.velocity.y, move.z);
        //    //if (Input.GetButton("Jump") && IsGrounded())
        //    //{
        //    //    PlayerBody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
        //    //    //PlayerBody.velocity.y = JumpForce;
        //    //}

        //    _rb.velocity += Physics.gravity  * Time.deltaTime * 2 ;
        //}

        private void CheckGrounded()
        {
            _isGrounded    = false;
            float height   = Mathf.Max(_collider.radius * 2f, _collider.height);
            Vector3 bottom = transform.TransformPoint(_collider.center - Vector3.up * height / 2f);
            float radius   = transform.TransformVector(_collider.radius, 0f, 0f).magnitude;
            Ray ray        = new(bottom + transform.up * 0.01f, -transform.up);
            if (Physics.Raycast(ray, out RaycastHit hit, radius * 5f))
            {
                float normalAngle = Vector3.Angle(hit.normal, transform.up);
                if (normalAngle < MaxSlope)
                {
                    float maxDist = radius / Mathf.Cos(Mathf.Deg2Rad * normalAngle) - radius + .02f;
                    if (hit.distance < maxDist)
                        _isGrounded = true;
                }
            }
        }

        private void Move()
        {
            if (_isGrounded)
            { 
                Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                var acc = Input.GetButton("Run") ? Speed * 1.5f : Speed;
                Vector3 vel = transform.TransformDirection(direction) * acc;
                _rb.AddForce(vel * Time.fixedDeltaTime,  ForceMode.VelocityChange);
            }
        }

        void Jump()
        {
            if (_isGrounded)
            {
                //_rb.velocity += transform.up * JumpSpeed;
                _rb.AddForce(transform.up * JumpSpeed, ForceMode.VelocityChange);
            }
        }

        void StepClimb()
        {
            if (!_isGrounded || _rb.velocity.z < 0.01f) return;
            float StepHeight = 0.50f;
            float height = Mathf.Max(_collider.radius * 2f, _collider.height);
            Vector3 lowerPos = transform.TransformPoint(_collider.center - Vector3.up * height / 2f);
            lowerPos.y += 0.21f;
            Vector3 higherPos = new (lowerPos.x, lowerPos.y + StepHeight + 0.2f, lowerPos.z);

            if (Physics.Raycast(lowerPos, transform.forward, out RaycastHit hit, _collider.radius + 0.1f))
            {
                Debug.DrawLine(lowerPos, hit.point);
                if (Physics.Raycast(higherPos, transform.forward, out hit, _collider.radius + 10.0f))
                {
                    Debug.DrawLine(higherPos, hit.point);
                    var hp = hit.point;
                    //_rb.AddForce(Vector3.up * 5, ForceMode.VelocityChange);
                    if (Physics.Raycast(hit.point, Vector3.up *-1, out hit, StepHeight))
                    {

                        var np = new Vector3(transform.position.x, hit.point.y + 0.05f, transform.position.z + _collider.radius * 1.5f);
                        Debug.DrawLine(hp, np);
                        _rb.position = np;
                        //_rb.position += new Vector3(0f, height * 0.5f, _collider.radius + 0.1f);
                        //_rb.position = new Vector3(_rb.position.x, hit.point.y, _rb.position.z);
                    }
                    //_rb.velocity += 2 * Speed * Time.fixedDeltaTime * Vector3.forward;
                    //_rb.AddForce(transform.forward, ForceMode.VelocityChange );
                }
            }

            //if (Physics.Raycast(lowerPos, transform.TransformDirection(1.5f, 0, 1), out hit, 0.1f))
            //{
            //    if (!Physics.Raycast(higherPos, transform.TransformDirection(1.5f, 0, 1), out hit, 0.2f))
            //    {
            //        _rb.position -= new Vector3(0f, -StepSmooth * Time.fixedDeltaTime, 0f);
            //    }
            //}

            //if (Physics.Raycast(lowerPos, transform.TransformDirection(-1.5f, 0, 1), out hit, 0.1f))
            //{
            //    if (!Physics.Raycast(higherPos, transform.TransformDirection(-1.5f, 0, 1), out hit, 0.2f))
            //    {
            //        _rb.position -= new Vector3(0f, -StepSmooth * Time.fixedDeltaTime, 0f);
            //    }
            //}
        }

        //void Update()
        //{
        //    float hMove = Input.GetAxisRaw("Horizontal");
        //    float vMove = Input.GetAxisRaw("Vertical");
        //    MoveDir = (hMove * transform.right + vMove * transform.forward + new Vector3(0, PlayerBody.velocity.y, 0)).normalized;
        //}

        void FixedUpdate()
        {
            CheckGrounded();
            Move();
            StepClimb();

            if (Input.GetButton("Jump"))
            {
                Jump();
            }

            _rb.AddForce(Physics.gravity, ForceMode.Acceleration);
        }
    }
}