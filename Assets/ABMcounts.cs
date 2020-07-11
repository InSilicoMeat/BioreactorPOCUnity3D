using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ABMcounts : MonoBehaviour
{
    DataReader _dr;
    // Start is called before the first frame update
    void Start()
    {
        _dr = GameObject.FindObjectOfType<DataReader>();
        gameObject.GetComponent<TextMeshProUGUI>().text = "Microcarriers: "+_dr.MicrocarrierCount()+"\nCells: "+_dr.CellCount();
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.GetComponent<TextMeshProUGUI>().text = "Microcarriers: " + _dr.MicrocarrierCount() + "\nCells: " + _dr.CellCount();
    }
}
