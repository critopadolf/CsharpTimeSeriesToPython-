import os, sys
sys.path.append('../')
from dataset import UnityRecording as unity_dataset
filepath = "ex.dataset"
recording = unity_dataset(filepath)
tensorsNames = recording.listNames()
print(tensorsNames)
dt = recording.get("dt")
print(dt)