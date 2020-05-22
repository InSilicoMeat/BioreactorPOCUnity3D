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
        Debug.Log("contact");
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
        }

        //if (collision.relativeVelocity.magnitude > 2)
            audioSource.Play();
    }
}