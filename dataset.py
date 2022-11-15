import os
import numpy as np

class UnityRecording:
    def __init__(self, filepath):
        with open(filepath, mode='rb') as file: # b is important -> binary
            fileContent = file.read()
            numTensors = int.from_bytes(fileContent[0:4], "little")

            
            offs = 4
            tensorInfo = []
            self.__tensor_dict__ = {}
            for x in range(numTensors):
                nameLength = int.from_bytes(fileContent[offs:offs+4], "little")
                offs += 4
                typeLength = int.from_bytes(fileContent[offs:offs+4], "little")
                offs += 4
                name = fileContent[offs:offs+nameLength].decode('ASCII')
                offs += nameLength
                typeOf = fileContent[offs:offs+typeLength].decode('ASCII')
                offs += typeLength
                
                shapeLength = int.from_bytes(fileContent[offs:offs+4], "little")
                offs += 4
                shape = np.frombuffer(fileContent[offs:offs+4*shapeLength], dtype=np.int32)
                offs += 4*shapeLength
                dataLength = int.from_bytes(fileContent[offs:offs+8], 'little')
                offs+=8
                _dict_ = {}
                _dict_['name'] = name
                _dict_['shape'] = shape
                _dict_['type'] = typeOf
                _dict_['bytesLength'] = dataLength
                tensorInfo.append(_dict_)
            for t in tensorInfo:
                byteCount = t['bytesLength']
                nxt = offs+byteCount
                bdata = fileContent[offs:nxt]
                flat_tensor = np.frombuffer(bdata, dtype=t['type'])
                self.__tensor_dict__[t['name']] = flat_tensor.reshape(t['shape'])
                offs = nxt
    def listNames(self):
        return self.__tensor_dict__.keys()
    def get(self, name):
        return self.__tensor_dict__[name]
    # end class





    