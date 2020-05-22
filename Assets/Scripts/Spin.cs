using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
    public float RPM;
    Rigidbody rb;
    Vector3 eulerAngleVelocity;
    // Start is called before the first frame update
    void Start()
    {
        if (RPM == 0f) RPM = 1.0f / 60;
        rb = gameObject.GetComponent<Rigidbody>();
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        eulerAngleVelocity = new Vector3((RPM / 60) * 360, 0, 0);
        Quaternion deltaRotation = Quaternion.Euler(eulerAngleVelocity * Time.deltaTime);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
}
