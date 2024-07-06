using System;
using UnityEngine;
using System.Collections;
using Assets.Sith.Utility;
using Assets.Sith.Gui;
using Assets.Sith.Game.Thing;

public class PlayerMouselook : MonoBehaviour {
    private Camera _camera;
    private Bounds _bounds;
    private SithThing _thing;

    public float LookSensitivity = 1.0f;
    public bool FirstPerson = true;

    [ConditionalHide("FirstPerson", true)]
    public Assets.Sith.Utility.Range PitchRange = new(-50, 70);

    [ConditionalHide("FirstPerson", true, true)]
    public Vector3 CameraOffset = new(0.0f, 0.65f, -2.0f);
    [ConditionalHide("FirstPerson", true, true)]
    public Vector3 CameraLookOffset = new (0.2f, 0.2f, 0.0f);
    [ConditionalHide("FirstPerson", true, true)]
    public float CameraFadeSpeed = 5.0f;

    // Use this for initialization
    void Start ()
    {
        _bounds = GetComponent<Collider>().bounds;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        _camera = GetComponentInChildren<Camera>();
        _thing = GetComponent<SithThing>();
    }

    float previousOpacityInterp = 0f;
    // Update is called once per frame
    void Update()
    {
        var horizDeltaAngle = Input.GetAxis("Mouse X") * LookSensitivity;
        var newHorizAngle = transform.localEulerAngles.y + horizDeltaAngle;
        transform.localRotation = Quaternion.Euler(0, newHorizAngle, 0);
        if (_camera)
        {
            if (FirstPerson)
            {
                var vertDeltaAngle = Input.GetAxis("Mouse Y") * LookSensitivity;
                var newVertAngle = _camera.transform.localEulerAngles.x - vertDeltaAngle;
                newVertAngle = (newVertAngle > 180) ? newVertAngle - 360 : newVertAngle;
                newVertAngle = Mathf.Clamp(newVertAngle, PitchRange.Min, PitchRange.Max);
                _camera.transform.localPosition = new Vector3(0, _bounds.size.y - 0.15f, 0.10f);
                _camera.transform.transform.rotation = Utils.ToThingOrientation(Quaternion.Euler(newVertAngle, newHorizAngle, 0));
            }
            else
            {
                var fadeTime = CameraFadeSpeed * Time.deltaTime;

                var offset = CameraOffset;
                var origin = transform.position + new Vector3(0, offset.y, 0);
                var maxdist = Mathf.Abs(offset.z);
                var lookAtPlayer = true;
                if (Physics.SphereCast(origin, _bounds.extents.z - 0.1f, transform.forward * -1, out RaycastHit hitInfo, maxdist))
                {
                    Debug.DrawLine(origin, hitInfo.point);
                    var distance   = (hitInfo.distance - maxdist);
                    var correction = Vector3.Normalize(offset) * distance;
                    offset += correction;

                    lookAtPlayer = hitInfo.distance >= 1.1f;
                    if (!lookAtPlayer)
                    {
                        if (_thing.opacity != 0.0f)
                        {
                            previousOpacityInterp += fadeTime;
                            _thing.opacity = Mathf.Lerp(1.0f, 0.0f, previousOpacityInterp);
                        }
                        else
                            previousOpacityInterp = 0.0f;

                    }
                    else if (_thing.opacity != 1.0f)
                    {
                        previousOpacityInterp += fadeTime;
                        _thing.opacity = _thing.opacity = Mathf.Lerp(0.0f, 1.0f, previousOpacityInterp);
                    }
                    else
                        previousOpacityInterp = 0.0f;
                }
                else
                {
                    if (_thing.opacity != 1.0f)
                    {
                        previousOpacityInterp += fadeTime;
                        _thing.opacity = _thing.opacity = Mathf.Lerp(0.0f, 1.0f, previousOpacityInterp);
                    }
                    else
                        previousOpacityInterp = 0.0f;
                }

                _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, offset, fadeTime);
                if (lookAtPlayer)
                {
                    _camera.transform.LookAt(transform.position + CameraLookOffset);
                }
            }
        }
    }
}
