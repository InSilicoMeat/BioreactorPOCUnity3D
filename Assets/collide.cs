using UnityEngine;
using System.Collections;

public class collide : MonoBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {

        foreach (ContactPoint contact in collision.contacts)
        {
            // Debug.DrawRay(contact.point, contact.normal, Color.red);
            Debug.Log("contact "+contact.point);
        }

        //if (collision.relativeVelocity.magnitude > 2)
            audioSource.Play();
    }
}