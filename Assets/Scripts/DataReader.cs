using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

// There are sophisticated packages for reading VTK data available from Kitware.
// This is a hack that will break if the structure of the data produced by
//   the biocellion model changes.
// DataReader loads data from the vtp files produced by biocellion for Paraview.
// The way the files are organized by biocellion is assumed to be a *.pvtp file
// that names the *.vtp files that contain the data, agent_<num>.pvtp
// where <num> is the timestep (in a field padded on left with zeroes).
// The line(s) in a pvtp file
// <Piece Source="agent_0_0_0_00000.vtp"/>
// identify those data files by name.
// Each vtp file provides data for a number of points:
// <Piece NumberOfPoints="1668" NumberOfVerts="0" NumberOfStrips="0" NumberOfPolys="0">
// and the offsets for the data are given in lines that follow:
// <PointData>
// <DataArray type = "Float64" Name="color" format="appended" offset="0"/>
// <DataArray type = "Float64" Name="radius" format="appended" offset="13348"/>
// <DataArray type = "Float64" Name="stress" format="appended" offset="26696"/>
// </PointData>
// <Points>
// <DataArray type = "Float64" NumberOfComponents="3" format="appended" offset="40044"/>
// </Points>
// The Float64 data is in Little Endian format and is preceded by the _ character.
// To accelerate file processing, we have partitioned the vtp files into a vtp.txt file containing
// the xml header information and a vtp.data.bytes file containing the raw data beginning with _.
// A script for splitting the files is in https://github.com/InSilicoMeat/Utilities-split-vtp-files
//
public class DataReader : MonoBehaviour
{
    public GameObject Cell, Microcarrier;
    public float FrameRate; //frames per second
    public double half_stress;

    public string DataPath; //set from unity editor when files are external
    TextAsset[] _all_files;

    float _timer; //time since last change in seconds
    int _frame_number; //ranges from 0 to number of pvtp files at DataPath
    int _num_frames;


    string[] _pvtp_files; //used when data files are external

    TextAsset[] _pvtp_assets; //used when data files are internal
    Dictionary<string, TextAsset> _textAssetD;

    Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
    Dictionary<int, Vector3> _velocities = new Dictionary<int, Vector3>();
    List<int> _microcarriers = new List<int>();

    // Start is called before the first frame update
    void Start()
    {
        _frame_number = 0;
        _timer = 0.0f;
        if (FrameRate == 0.0) FrameRate = 1;
        //read in first pvtp file
        if (DataPath.Length == 0) DataPath = "output60b";
        _all_files = Resources.LoadAll<TextAsset>(DataPath);
        _textAssetD = new Dictionary<string, TextAsset>();
        List<TextAsset> _pvtp_file_list = new List<TextAsset>();
        foreach (TextAsset t in _all_files)
        {
            if (t.name.Contains(".pvtp")) _pvtp_file_list.Add(t);
            _textAssetD.Add(t.name, t);
        }
        _pvtp_assets = _pvtp_file_list.ToArray();
        _num_frames = _pvtp_assets.Length;

        Debug.Log("Found " + _num_frames + " pvtp files\n");
        ProcessNextAsset();
    }

    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer * FrameRate > 1)
        {
            do
            {
                _timer -= 1.0f / FrameRate;
                //advance to next vtu file
                if (++_frame_number >= _num_frames) //reached end; start over
                {
                    _frame_number = 0;
                    foreach (GameObject go in _objects.Values) Destroy(go);
                    _objects.Clear();
                    _velocities.Clear();
                    _microcarriers.Clear();
                }
            }
            while (_timer * FrameRate > 1);
            ProcessNextAsset();
        }
    }

    void ProcessNextAsset()
    {
        if (_num_frames < 1) return;
        Debug.Log("Processing file " + _pvtp_assets[_frame_number].name + " for frame " + _frame_number + "\n");

        string line;
        int color_offset = 0, radius_offset = 0, stress_offset = 0, id_offset = 0, point_offset = 0;
        int vx_offset = 0, vy_offset = 0, vz_offset = 0;
        List<TextAsset> vtp_assets;
        List<TextAsset> vtp_data_assets;

        vtp_assets = new List<TextAsset>();
        vtp_data_assets = new List<TextAsset>();

        string s = _pvtp_assets[_frame_number].text;

        string[] lines = Regex.Split(s, "\n|\r|\r\n");
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("Source"))
            {
                Match new_file = Regex.Match(lines[i], "\"([^\"]*)\"");
                string new_file_name = new_file.Value.Replace("\"", "");
                TextAsset val;
                var res = _textAssetD.TryGetValue(new_file_name, out val);

                if (!res) Debug.Log("Source file " + new_file_name + " specified in " + _pvtp_assets[_frame_number].name + " not found.");
                vtp_assets.Add(val);

                res = _textAssetD.TryGetValue(new_file_name + ".data", out val);
                if (!res) Debug.Log("Data file " + new_file_name + ".data specified in " + _pvtp_assets[_frame_number].name + " not found.");
                vtp_data_assets.Add(val);
            }
        }
        // here is where we'd like to "unload" the pvtp file resource from memory, though it's small
        int num_vtp_files = vtp_assets.Count;

        for (int vid = 0; vid < vtp_assets.Count; vid++)
        {
            TextAsset vtp_asset = vtp_assets[vid];
            TextAsset vtp_data_asset = vtp_data_assets[vid];

            int num_points = 0;
            //s = System.Text.Encoding.Default.GetString(vtp_asset.bytes);
            s = vtp_asset.ToString();
            const string regex = "\"([^\"]*)\""; //quoted items

            lines = Regex.Split(s, "\n|\r|\r\n");
            for (int i = 0; i < lines.Length; i++)
            {
                line = lines[i];
                Debug.Log(line);
                if (line.Contains("NumberOfPoints"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[0].Value.Replace("\"", ""));
                    num_points = Int32.Parse(co[0].Value.Replace("\"", ""));
                }
                else if (line.Contains("color"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    color_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("radius"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    radius_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("stress"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    stress_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("id"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    id_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("NumberOfComponents"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    point_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                    break;
                }
                else if (line.Contains("vx"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    vx_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("vy"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    vy_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("vz"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    vz_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
            }

            //done with txt file, now read data

            byte[] b = vtp_data_asset.bytes;

            if (Convert.ToChar(b[0]) != '_') Debug.Log("DataReader misalignment at 1");

            int c_file_posn = 5 + color_offset;
            int r_file_posn = 5 + radius_offset;
            int i_file_posn = 5 + id_offset;
            int p_file_posn = 5 + point_offset;
            int s_file_posn = 5 + stress_offset;
            int vx_file_posn = 5 + vx_offset;
            int vy_file_posn = 5 + vy_offset;
            int vz_file_posn = 5 + vz_offset;

            // An assumption is that an object whose id is less than the number of
            //   agents already added to the scene must already be in the scene.
            for (int i = 0; i < num_points; i++)
            {
                int id = Convert.ToInt32(BitConverter.ToDouble(b, i_file_posn)); i_file_posn += 8;
                // biocellion's (x, y, z) is Unity's (z, x, y)
                double z = BitConverter.ToDouble(b, p_file_posn) / 1000 - 27.5; p_file_posn += 8;
                double x = BitConverter.ToDouble(b, p_file_posn) / 1000 - 27.5; p_file_posn += 8;
                double y = BitConverter.ToDouble(b, p_file_posn) / 1000; p_file_posn += 8;
                Vector3 p = new Vector3(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
                float r = Convert.ToSingle(BitConverter.ToDouble(b, r_file_posn)) * 2 / 1000; r_file_posn += 8;
                int c = Convert.ToInt32(BitConverter.ToDouble(b, c_file_posn)); c_file_posn += 8;
                double stress = Math.Abs(BitConverter.ToDouble(b, s_file_posn)); s_file_posn += 8;
                stress = stress / (half_stress + stress);
                // biocellion's (vx, vy, vz) is Unity's (vz, vx, vy)
                float vz = Convert.ToSingle(BitConverter.ToDouble(b, vx_file_posn))/1000;  vx_file_posn += 8;
                float vx = Convert.ToSingle(BitConverter.ToDouble(b, vy_file_posn))/1000; vy_file_posn += 8;
                float vy = Convert.ToSingle(BitConverter.ToDouble(b, vz_file_posn))/1000; vz_file_posn += 8;
                Vector3 v = new Vector3(vx, vy, vz);

                bool exists = _objects.TryGetValue(id, out GameObject obj);
                if (exists)
                {
                    //can we compare type to expected type? if originally a cell is this still a cell?
                    obj.transform.position = p;
                    obj.transform.localScale = new Vector3(r, r, r);
                    _velocities[id] = v;
                }
                else
                {
                    //Debug.Log("Add " + (c==1? "cell":"microcarrier") + " at " + p);
                    if (c == 1) obj = Instantiate(Cell, p, Quaternion.identity);
                    else
                    {
                        obj = Instantiate(Microcarrier, p, Quaternion.identity);
                        _microcarriers.Add(id); //the biocellion id of microcarrier doesn't matter
                    }
                    obj.transform.localScale = new Vector3(r, r, r);
                    _objects.Add(id, obj);
                    _velocities.Add(id, v);
                }
                if (c == 1) //color only the cells, not the microcarriers
                {
                    var col = obj.GetComponent<Renderer>().material.color;
                    col.g = col.b = 1 - Convert.ToSingle(stress);
                    obj.GetComponent<Renderer>().material.color = col;
                }
            }
            // unload the assets
        }
    }
    public Vector3 MicrocarrierPosition(int n)
    {
        int i = n % _microcarriers.Count;
        int id = _microcarriers[i];
        return _objects[id].transform.position;
    }
    public Vector3 MicrocarrierVelocity(int n)
    {
        int i = n % _microcarriers.Count;
        int id = _microcarriers[i];
        return _velocities[id];

    }
}