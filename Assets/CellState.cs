using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CellStates { Quiescent, Growing, Proliferating, Dead }
public class CellState : MonoBehaviour
{
    public CellStates state = CellStates.Quiescent;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
