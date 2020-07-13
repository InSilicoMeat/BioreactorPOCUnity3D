using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Content : MonoBehaviour
{
    string _menu_text = "MENU: <esc> toggles\nFOCUS: f advances\nUNFOCUS: u\nFORWARD: {w, W}\nBACKWARD: {s, S}\nLEFT: {a, A}\nRIGHT: {d, D}\nROTATE: <ctrl> mouse\nSPEED: +/-\nPAUSE: <spc> toggles\nHOME: h";
    bool _short = true;
    GameObject _go;
    // Start is called before the first frame update
    void Start()
    {
        _go = gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _short = _short ? false : true;
            if (_short) _go.GetComponent<TextMeshProUGUI>().text = "MENU: <esc> toggles";
            else gameObject.GetComponent<TextMeshProUGUI>().text = _menu_text;
        }

    }
}
