using System;
using UnityEngine;
using System.Collections;

public class PlayerMouselook : MonoBehaviour {
    private Transform _rotatePivot;
    public float LookSensitivity = 1.0f;

    // Use this for initialization
	void Start ()
	{
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
	    _rotatePivot = transform.Find("RotatePivot");
	}
	
	// Update is called once per frame
	void Update ()
	{
	    var horizDeltaAngle = Input.GetAxis("Mouse X")* LookSensitivity;
        var vertDeltaAngle = Input.GetAxis("Mouse Y")* LookSensitivity;

        var newVertAngle = _rotatePivot.transform.localEulerAngles.x - vertDeltaAngle;
        newVertAngle = (newVertAngle > 180) ? newVertAngle - 360 : newVertAngle;
	    newVertAngle = Mathf.Clamp(newVertAngle, -80, 80);

	    _rotatePivot.transform.localRotation = Quaternion.Euler(newVertAngle, 0, 0);

	    var newHorizAngle = transform.localEulerAngles.y + horizDeltaAngle;
        transform.localRotation = Quaternion.Euler(0, newHorizAngle, 0);
	}

    void OnGUI()
    {
        //GUILayout.Label("Horiz: " + angle);
        //GUILayout.Label("Vert: " + transform.localEulerAngles.y);
    }
}
