using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dataset
{
    private struct data
    {
        public int length;
        int[] shape;
        public System.IO.BinaryWriter bWrite;
        public string _type_;
        public int byteLength;
        public long bytesLength; // total bytes in file
        public string filepath;
        public data(string filpath, int l, int[] s, string _type, int bLength)
        {
            _type_ = _type; // 5 chars => 5 bytes
            length = l;
            shape = s; 
            filepath = filpath;
            bWrite = new System.IO.BinaryWriter(System.IO.File.Open(filepath, System.IO.FileMode.Append));
            
            byteLength = bLength; // number of bytes per entry
            bytesLength = 0; // total data length in bytes
        }
        public void incrementBytesLength(long k)
        {
            bytesLength+=k;
        }
        public void incrementTensorLength()
        {
            length++;
        }
        public byte[] toBytes(string name)
        {
            // name[32], type[32], shapeLength[4], length[4], shape[4*x]
            // char[32], char[32], int, int, long



            byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(name);  // # name of the data
            byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(_type_); // # type of the data
            byte[] shapeLenBytes = System.BitConverter.GetBytes( (1 + shape.Length) ); // # shape of the data
            byte[] lengthBytes = System.BitConverter.GetBytes( (length) ); // shape[0]
            byte[] dataLength = System.BitConverter.GetBytes(bytesLength); // number of bytes in the actual data
            byte[] shapeBytes = new byte[shape.Length*4];
            for(int x = 0; x < shape.Length; x++)
            {
                System.Buffer.BlockCopy( System.BitConverter.GetBytes(shape[x]), 0, shapeBytes, x*4, 4 );
            }
            byte[] nameBytesLengthBytes = System.BitConverter.GetBytes(nameBytes.Length);
            byte[] typeBytesLengthBytes = System.BitConverter.GetBytes(typeBytes.Length);

            long totalLength = nameBytesLengthBytes.Length + typeBytesLengthBytes.Length + nameBytes.Length;
            totalLength += typeBytes.Length + shapeLenBytes.Length + lengthBytes.Length + shapeBytes.Length + dataLength.Length;
            byte[] b = new byte[totalLength];
            int k = 0;


            // length of tensor name int32
            System.Buffer.BlockCopy(nameBytesLengthBytes,     0, b, k, nameBytesLengthBytes.Length );  k += nameBytesLengthBytes.Length;
            // length of type name int32
            System.Buffer.BlockCopy(typeBytesLengthBytes,     0, b, k, typeBytesLengthBytes.Length );  k += typeBytesLengthBytes.Length;
            // name char array
            System.Buffer.BlockCopy(nameBytes,                0, b, k, nameBytes.Length            );  k += nameBytes.Length;
            // type char array
            System.Buffer.BlockCopy(typeBytes,                0, b, k, typeBytes.Length            );  k += typeBytes.Length;
            // length of shape array int32
            System.Buffer.BlockCopy(shapeLenBytes,            0, b, k, shapeLenBytes.Length        );  k += shapeLenBytes.Length;
            // number of appended tensors int32
            System.Buffer.BlockCopy(lengthBytes,              0, b, k, lengthBytes.Length          );  k += lengthBytes.Length;
            // inner shape array [int32]
            System.Buffer.BlockCopy(shapeBytes,               0, b, k, shapeBytes.Length           );  k += shapeBytes.Length;
            // int64 length of data
            System.Buffer.BlockCopy(dataLength,               0, b, k, dataLength.Length           );  k += dataLength.Length;
            
            return b;
        }
    }


    private Dictionary<string, data> dict;
    private string tmpFolderName;
    private string filename;
    private int dictLength = 0;
    public bool isOpen = false;
    System.IO.DirectoryInfo tmpDirInfo;
    public dataset(string fileName)
    {
        dict = new Dictionary<string, data>();
        // create temp folder
        tmpFolderName = fileName + "_tmp"; // temporary folder to store files in before concatenating
        filename = fileName; // final file name
        tmpDirInfo = System.IO.Directory.CreateDirectory(tmpFolderName); // create temporary directory
        isOpen = true;
    }

    public void create(string name, string type, int[] sz, int byteLength)
    {
        if(name == "__header__")
        {
            Debug.LogError("cannot create file of name __header__");
            return;    
        }
        if(!dict.ContainsKey(name))
        {
            Debug.Log("creating " + name + " of type " + type);
            string filepath = tmpFolderName + "/" + name; // filepath of temporary file
            data d = new data(filepath, 0, sz, type, byteLength); // create data object
            dict.Add(name, d);  // place data object in dict
            dictLength++; // increment length of dictionary
        }
        else
        {
            Debug.LogError("create call to dataset already contains name: " + name);
        }
    }
    // [name] name of the tensor, [mat] tensor data, [byteLength] length of each input in bytes, 
    // [toBytes] function to convert single instance to byte array
    public void append<T>(string name, T[] mat, System.Func<T, byte[]> toBytes)
    {
        try
        {
            data d = dict[name]; 
            // append to temp file
            byte[] byteArray = new byte[d.byteLength * mat.Length]; // byte array length is the length returned by toBytes() times the length of the input vector
            for(int x = 0; x < mat.Length; x++)
            {
                byte[] bytes = toBytes(mat[x]);
                System.Buffer.BlockCopy(bytes, 0, byteArray, x*d.byteLength, d.byteLength);
                d.bytesLength += bytes.Length; // tally total bytes in file
            }
            d.bWrite.Write(byteArray);
            d.length+=1;
            dict[name] = d;
        }
        catch (KeyNotFoundException)
        {
            Debug.LogError("Error In dataset.append: Key " + name + " not found");
        }
    }

    public void close()
    {
        // get byte address of each file
        // merge files, delete tmp folder
        string headerFilePath = tmpFolderName + "/" + "__header__"; // header file path
        System.IO.BinaryWriter headerFileWriter = new System.IO.BinaryWriter(System.IO.File.Open(headerFilePath, System.IO.FileMode.Append)); // open header file
        byte[] dictLengthBytes = System.BitConverter.GetBytes( dictLength ); // get bytes for number of dict entries
        headerFileWriter.Write(dictLengthBytes); // write the number of dictionary entries to the header
        // write each data entry with headerFileWriter
        List<string> srcFileNames = new List<string>(); // list of files to concatenate
        srcFileNames.Add(headerFilePath); // add the header file to the files to concatenate

        // write the data entries to the header file
        foreach(KeyValuePair<string, data> entry in dict)
        {
            entry.Value.bWrite.Close(); // close writer for this datastream
            byte[] b = entry.Value.toBytes(entry.Key); // get sub-header bytes for data (not actual data, just type, size, etc)
            headerFileWriter.Write(b); // write the sub-header
            srcFileNames.Add(entry.Value.filepath); // add filename to list of files to concatenate
        }
        headerFileWriter.Close();
        
        // concatenate the files
        using (System.IO.Stream destStream = System.IO.File.OpenWrite(filename))
        {
            foreach (string srcFileName in srcFileNames)
            {
                using (System.IO.Stream srcStream = System.IO.File.OpenRead(srcFileName))
                {
                    srcStream.CopyTo(destStream);
                }
            }
        }
        

        refresh();
    }
    private void refresh()
    {
        dict = new Dictionary<string, data>();
        tmpDirInfo.Delete(true);
        filename = "";
        dictLength = 0;
        tmpFolderName = "";
        isOpen = false;
    }
}
