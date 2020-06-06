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
// DataReader loads data from the vtp files produced by biocellion for Paraview
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
public class DataReader : MonoBehaviour
{
    public GameObject Cell, Microcarrier;
    public float FrameRate = 10f; //frames per second

    public bool data_files_are_external = false; //are data files in Resources?
    public string DataPath; //set from unity editor when files are external
    TextAsset[] _all_files;

    float _timer; //time since last change in seconds
    int _frame_number; //ranges from 0 to number of pvtp files at DataPath
    int _num_frames;


    string[] _pvtp_files; //used when data files are external

    TextAsset[] _pvtp_assets; //used when data files are internal
    Dictionary<string, TextAsset> _textAssetD;

    Dictionary<Int32, GameObject> _objects = new Dictionary<Int32, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        _frame_number = 0;
        _timer = 0.0f;
        //read in first pvtp file
        if (data_files_are_external)
        {
            DataPath = "/Users/simonkahan/CMMC POC/Assets/Resources/output60b/";
            _pvtp_files = Directory.GetFiles(DataPath, "*.pvtp.bytes", SearchOption.TopDirectoryOnly);
            _num_frames = _pvtp_files.Length;
        }
        else
        { //gotta be a simpler way than this...
            DataPath = Application.dataPath + "/Resources/" + "output60b" + "/";
            _all_files = Resources.LoadAll<TextAsset>("output60b");
            _textAssetD = new Dictionary<string, TextAsset>();
            List<TextAsset> _pvtp_file_list = new List<TextAsset>();
            foreach (TextAsset t in _all_files)
            {
                if (t.name.Contains(".pvtp")) _pvtp_file_list.Add(t);
                _textAssetD.Add(t.name, t);
            }
            _pvtp_assets = _pvtp_file_list.ToArray();
            _num_frames = _pvtp_assets.Length;
        }
        Debug.Log("Found " + _num_frames + " pvtp files\n");
        if (data_files_are_external)
        {
            ProcessNextFile();
        }
        else ProcessNextAsset();
    }

    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer * FrameRate > 1)
        {
            _timer -= 1.0f / FrameRate;
            //advance to next vtu file
            _frame_number++; if (_frame_number >= _num_frames)
            {
                _frame_number = 0;
                foreach (GameObject go in _objects.Values) Destroy(go);
                _objects.Clear();
            }
            if (data_files_are_external)
            {
                ProcessNextFile();
            }
            else ProcessNextAsset();
        }
    }

    void ProcessNextAsset()
    {
        if (_num_frames < 1) return;
        Debug.Log("Processing file " + _pvtp_assets[_frame_number].name + " for frame " + _frame_number + "\n");

        string line;
        int color_offset = 0, radius_offset = 0, stress_offset = 0, id_offset = 0, point_offset = 0;
        List<TextAsset> vtp_assets;

        vtp_assets = new List<TextAsset>();
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
            }
        }
        // here is where we'd like to "unload" the pvtp file resource from memory, though it's small
        int num_vtp_files = vtp_assets.Count;

        Debug.Log("Found " + num_vtp_files + " vtp files\n");

        foreach (TextAsset vtp_asset in vtp_assets)
        {
            Debug.Log("Processing " + vtp_asset.name);

            int file_posn = 0;
            int num_points = 0;
            s = System.Text.Encoding.Default.GetString(vtp_asset.bytes);
            //s = vtp_asset.text; //doesn't work
            Debug.Log("Examining " + vtp_asset.name + ": has " + s.Length + " chars");
            const string regex = "\"([^\"]*)\""; //quoted items
            lines = Regex.Split(s, "\n|\r|\r\n");
            for (int i = 0; i < lines.Length; i++)
            {
                line = lines[i];
                file_posn += line.Length + 1;
                if (line.Contains("NumberOfPoints"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[0].Value.Replace("\"", ""));
                    num_points = Int32.Parse(co[0].Value.Replace("\"", ""));
                }
                else if (line.Contains("color"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    color_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("radius"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    radius_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("stress"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    stress_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("id"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    id_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("NumberOfComponents"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    point_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                    break;
                }
            }

            while (file_posn < s.Length && (s[file_posn] != '_')) file_posn++;
            if (file_posn == s.Length) Debug.Log("Reached end of vtp file unexpectedly");
            file_posn++;

            byte[] b = vtp_asset.bytes;

            Debug.Log("b has length " + b.Length);
            if (Convert.ToChar(b[file_posn - 1]) != '_') Debug.Log("DataReader misalignment at " + file_posn);
            
            // We have reached the binary data, since it always begins just after the
            //   first _ following the header info.

            int c_file_posn = file_posn + 4 + color_offset;
            int r_file_posn = file_posn + 4 + radius_offset;
            int i_file_posn = file_posn + 4 + id_offset;
            int p_file_posn = file_posn + 4 + point_offset;

            // An assumption is that an object whose id is less than the number of
            //   agents already added to the scene must already be in the scene.

            for (int i = 0; i < num_points; i++)
            {
                int id = Convert.ToInt32(BitConverter.ToDouble(b, i_file_posn)); i_file_posn += 8;
                double z = BitConverter.ToDouble(b, p_file_posn) / 1000 - 27.5; p_file_posn += 8;
                double x = BitConverter.ToDouble(b, p_file_posn) / 1000 - 27.5; p_file_posn += 8;
                double y = BitConverter.ToDouble(b, p_file_posn) / 1000; p_file_posn += 8;
                Vector3 p = new Vector3(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
                float r = Convert.ToSingle(BitConverter.ToDouble(b, r_file_posn)) * 2 / 1000; r_file_posn += 8;
                int c = Convert.ToInt32(BitConverter.ToDouble(b, c_file_posn)); c_file_posn += 8;

                if (_objects.TryGetValue(id, out GameObject obj))
                {
                    //can we compare type to expected type? if originally a cell is this still a cell?
                    obj.transform.position = p;
                    obj.transform.localScale = new Vector3(r, r, r);
                }
                else
                {
                    //Debug.Log("Add " + (c==1? "cell":"microcarrier") + " at " + p);
                    if (c == 1) obj = Instantiate(Cell, p, Quaternion.identity);
                    else obj = Instantiate(Microcarrier, p, Quaternion.identity);
                    obj.transform.localScale = new Vector3(r, r, r);
                    _objects.Add(id, obj);
                }
            }
            // unload the assets
        }
    }

    void ProcessNextFile()
    {
        if (_num_frames < 1) return;
        Debug.Log("Processing file for frame " + _frame_number + "\n");

        string line;
        int color_offset = 0, radius_offset = 0, stress_offset = 0, id_offset = 0, point_offset = 0;
        List<string> vtp_files;

        vtp_files = new List<string>();
        System.IO.StreamReader file = new System.IO.StreamReader(_pvtp_files[_frame_number]);
        while ((line = file.ReadLine()) != null)
        {
            if (line.Contains("Source"))
            {
                Match new_file = Regex.Match(line, "\"([^\"]*)\"");
                vtp_files.Add(new_file.Value.Replace("\"", "") + ".bytes");
            }
        }
        file.Close();
        int num_vtp_files = vtp_files.Count;
        Debug.Log("Found " + num_vtp_files + " vtp files\n");

        foreach (string fname in vtp_files)
        {
            Debug.Log("Processing " + fname);

            int file_posn = 0;
            int num_points = 0;
            file = new System.IO.StreamReader(DataPath + fname);
            const string regex = "\"([^\"]*)\""; //quoted items
            while ((line = file.ReadLine()) != null)
            {
                file_posn += line.Length + 1;
                if (line.Contains("NumberOfPoints"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[0].Value.Replace("\"", ""));
                    num_points = Int32.Parse(co[0].Value.Replace("\"", ""));
                }
                else if (line.Contains("color"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    color_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("radius"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    radius_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("stress"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    stress_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("id"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    id_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                }
                else if (line.Contains("NumberOfComponents"))
                {
                    MatchCollection co = Regex.Matches(line, regex);
                    //Debug.Log(line + ":" + co[3].Value.Replace("\"", ""));
                    point_offset = Int32.Parse(co[3].Value.Replace("\"", ""));
                    break;
                }
            }

            int ch;

            while ((ch = file.Read()) != -1)
            {
                file_posn++;
                if (ch == '_') break;
            }
            if (ch == -1) Debug.Log("Reached end of vtp file unexpectedly");

            // We have reached the binary data, since it always begins just after the
            //   first _ following the header info.
            // So, we switch to a binary reader.
            //   Discard the buffered data, believing that it forces the position
            //   back to the current read offset.
            //   Pass this offset to the BinaryReader as place to begin reading.
            file.Close();
            BinaryReader cfile = new BinaryReader(File.Open(DataPath + fname,
                                       FileMode.Open, FileAccess.Read, FileShare.Read));
            BinaryReader rfile = new BinaryReader(File.Open(DataPath + fname,
                                       FileMode.Open, FileAccess.Read, FileShare.Read));
            BinaryReader sfile = new BinaryReader(File.Open(DataPath + fname,
                                       FileMode.Open, FileAccess.Read, FileShare.Read));
            BinaryReader ifile = new BinaryReader(File.Open(DataPath + fname,
                                       FileMode.Open, FileAccess.Read, FileShare.Read));
            BinaryReader pfile = new BinaryReader(File.Open(DataPath + fname,
                                       FileMode.Open, FileAccess.Read, FileShare.Read));
            cfile.BaseStream.Seek(file_posn - 1, SeekOrigin.Begin);
            ch = cfile.ReadChar();
            if (ch != '_') Debug.Log("DataReader misalignment\n");

            cfile.BaseStream.Seek(file_posn + 4 + color_offset, SeekOrigin.Begin);
            rfile.BaseStream.Seek(file_posn + 4 + radius_offset, SeekOrigin.Begin);
            //sfile.BaseStream.Seek(file_posn + 4 + stress_offset, SeekOrigin.Begin);
            ifile.BaseStream.Seek(file_posn + 4 + id_offset, SeekOrigin.Begin);
            pfile.BaseStream.Seek(file_posn + 4 + point_offset, SeekOrigin.Begin);

            // An assumption is that an object whose id is less than the number of
            //   agents already added to the scene must already be in the scene.

            for (int i = 0; i < num_points; i++)
            {
                int id = Convert.ToInt32(ifile.ReadDouble());
                double z = pfile.ReadDouble() / 1000 - 27.5; //conv micrometer to mm
                double x = pfile.ReadDouble() / 1000 - 27.5; //  and adjust origin
                double y = pfile.ReadDouble() / 1000;
                Vector3 p = new Vector3(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
                float r = Convert.ToSingle(rfile.ReadDouble()) * 2 / 1000;
                int c = Convert.ToInt32(cfile.ReadDouble());

                if (_objects.TryGetValue(id, out GameObject obj))
                {
                    //sfile.BaseStream.Seek(8, SeekOrigin.Current);
                    //can we compare type to expected type? if originally a cell is this still a cell?
                    obj.transform.position = p;
                    obj.transform.localScale = new Vector3(r, r, r);
                }
                else
                {
                    if (c == 1) obj = Instantiate(Cell, p, Quaternion.identity);
                    else obj = Instantiate(Microcarrier, p, Quaternion.identity);
                    obj.transform.localScale = new Vector3(r, r, r);
                    _objects.Add(id, obj);
                }
            }

            cfile.Close(); rfile.Close(); //sfile.Close();
            ifile.Close(); pfile.Close();
        }
    }
}