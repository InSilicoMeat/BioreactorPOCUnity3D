# BioreactorPOCUnity3D
Show animation from VTK files produced by ABM-Microcarriers model

Requires the folder of data created by running the ABM-Microcarriers model using biocellion.

Two possibilities for ingesting that data by the DataReader script:

1. You are running only on your own computer, whether from the unity editor or from your browser. You can set the public variable DataReader::data_files_are_external to true and set the DataPath in the corresponding branch of DataReader::Start to the absolute path of the directory as in

   DataPath = "/Users/simonkahan/CMMC POC/Assets/Resources/output60b/";

2. In addition to running on your own computer, you'd like to install the binary elsewhere, say, as a web demo on simmer.io. You can set DataReader::data_files_are_external to false, import the directory using the unity editor into Resources, and set the name of that directory in the second branch of the DataReader::Start, as in

   DataPath = Application.dataPath + "/Resources/" + "output60b" + "/";  
   \_all_files = Resources.LoadAll<TextAsset>("output60b");

