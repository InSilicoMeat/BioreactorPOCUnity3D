using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CameraController : MonoBehaviour
{
    public GameObject gobject;
    public float CameraSpeed = .1f;
    private Vector3 offset; Quaternion rot_offset;
    // Start is called before the first frame update
    void Start()
    {
        offset = transform.position - new Vector3(0, 0, 0);
        rot_offset = transform.rotation * gobject.transform.rotation;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //transform.position = gobject.transform.position + offset;
        //transform.rotation = gobject.transform.rotation * rot_offset;
        transform.position += new Vector3(CameraSpeed, 0, 0);

    }
}
