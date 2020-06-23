using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CameraController : MonoBehaviour
{

    /*
    Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.  
    Converted to C# 27-02-13 - no credit wanted.
    Simple flycam I made, since I couldn't find any others made public.  
    Made simple to use (drag and drop, done) for regular keyboard layout  
    wasd : basic movement
    shift : Makes camera accelerate
    space : Moves camera on X and Z axis only.  So camera doesn't gain any height*/
    // Modified by Simon Kahan:
    //  space - pauses
    //  f - focuses on "next" microcarrier
    //  u - unfocuses on microcarriers
    //  + speeds things up
    //  - slows things down
    //  
    public float nominal_lag_distance = 10; //when focused, distance behind microcarrier 

    float mainSpeed = 100.0f; //regular speed
    float shiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
    float maxShift = 1000.0f; //Maximum speed when holdin gshift
    float camSens = .1f; //How sensitive it with mouse
    private Vector3 freeMouse; //the origin for angle displacement when shift is pressed
    private float totalRun = 1.0f;

    bool _focused = false;
    int _microcarrier_id = 0;
    float _lag_distance;

    bool _paused = false;
    Spin _spin;
    DataReader _dataReader;
    float _saved_RPM;
    float _saved_FrameRate;

    private void Start()
    {
        freeMouse = new Vector3(0, 0, 0); //arbitrary
        _spin = GameObject.Find("StirRod").GetComponent<Spin>();
        _dataReader = GameObject.Find("DataReader").GetComponent<DataReader>();
        _lag_distance = nominal_lag_distance;
  
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            transform.position = new Vector3(-1000.0f, 27.6f, 0.4f);
            transform.eulerAngles = new Vector3(0f, 90f, 0f);
            _focused = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_paused)
            {
                _paused = false;
                _spin.RPM = _saved_RPM;
                _dataReader.FrameRate = _saved_FrameRate;
            }
            else
            {
                _paused = true;
                _saved_RPM = _spin.RPM;
                _spin.RPM = 0f;
                _saved_FrameRate = _dataReader.FrameRate;
                _dataReader.FrameRate = 0f;
            }
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            _dataReader.FrameRate /= 2;
            _spin.RPM /= 2;
        }
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            _dataReader.FrameRate *= 2;
            _spin.RPM *= 2;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            _focused = true;
            _microcarrier_id++;
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            _focused = false;
            _lag_distance = nominal_lag_distance;
        }

        if (_focused)
        {
            Vector3 mp = _dataReader.MicrocarrierPosition(_microcarrier_id);
            Vector3 mv = _dataReader.MicrocarrierVelocity(_microcarrier_id);
            transform.position = mp - mv.normalized * _lag_distance;
            transform.LookAt(mp);
            if (Input.GetKeyDown(KeyCode.W))
            {
                _lag_distance /= 2;
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                _lag_distance *= 2;
            }
        }
        else
        {

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                Debug.Log("contrl");
                Vector3 diffMouse = Input.mousePosition - freeMouse;
                Debug.Log(Input.mousePosition);
                diffMouse = new Vector3(-diffMouse.y * camSens, diffMouse.x * camSens, 0);
                diffMouse = new Vector3(transform.eulerAngles.x + diffMouse.x, transform.eulerAngles.y + diffMouse.y, 0);

                transform.eulerAngles = diffMouse;
                //Mouse  camera angle done.
            }

            //Keyboard commands
            Vector3 p = GetBaseInput();
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                Debug.Log("shift");
                totalRun += Time.deltaTime;
                p = p * totalRun * shiftAdd;
                p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
                p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
                p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
            }
            else
            {
                totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                p = p * mainSpeed;
            }


            p = p * Time.deltaTime;
            Vector3 newPosition = transform.position;
            if (Input.GetKey(KeyCode.Space))
            { //If player wants to move on X and Z axis only
                transform.Translate(p);
                newPosition.x = transform.position.x;
                newPosition.z = transform.position.z;
                transform.position = newPosition;
            }
            else
            {
                transform.Translate(p);
            }

            freeMouse = Input.mousePosition;
        }
    }

    private Vector3 GetBaseInput()
    { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }
        return p_Velocity;
    }
}