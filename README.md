# CsharpTimeSeriesToPython-
api for simple tensor file format. Similar to HDM5, but built for streaming tensorss to a file one frame at a time. 
Temporary files are created for each tensor, then concatenated when the close function is called. 

Example usage (Unity,  C#)
```C#
using UnityEngine;
using System;

public class testRecording : MonoBehaviour
{
    public string filepath;
    dataset ds;
    // Start is called before the first frame update
    void Start()
    {
        ds = new dataset(filepath); // create dataset file api manager
        int[] dtShape = {20, 2}; // define the shape of the tensor, actual shape is [None, 20, 2]
        ds.create("dt","float32", dtShape, 8); // 8 is the length of each Vector2 in bytes
    }
    // have to define a method to change your datatype to bytes, I'm sorry
    public byte[] Vector2ToBytes(Vector2 lbl)
    {
        byte[] byteArray = new byte[8];
        Buffer.BlockCopy( BitConverter.GetBytes( lbl.x ), 0, byteArray, 0,  4 );
        Buffer.BlockCopy( BitConverter.GetBytes( lbl.y ), 0, byteArray, 4,  4 );
        return byteArray;
    }
    // Update is called once per frame
    void Update()
    {
        // need to add ability to pass 1 dimensional float array, and automatically reshape to specified shape
        Vector2[] pos = new Vector2[20];
        for(int x = 0; x < pos.Length; x++)
        {
            pos[x].x = 0;
            pos[x].y = Time.deltaTime;
        }
        // pass in the tensor name, tensor, and function to convert a single element to bytes
        ds.append<Vector2>("dt", pos, Vector2ToBytes); 
    }

    public void OnDestroy()
	{
        ds.close();
	}
}
```

Example usage (python)

```python
import os, sys
sys.path.append('../')
from dataset import UnityRecording as unity_dataset
filepath = "ex.dataset"
recording = unity_dataset(filepath)
tensorsNames = recording.listNames() # list all available time series tensors
print(tensorsNames)
dt = recording.get("dt") # get the dt tensor
print(dt)
```
