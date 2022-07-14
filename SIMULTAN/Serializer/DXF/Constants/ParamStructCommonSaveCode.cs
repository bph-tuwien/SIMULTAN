using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    // ALL SAVE CODES
    // [0    - 1000]: DXF Specs and general custom codes
    // [1001 - 1100]: Parameter
    // [1101 - 1300]: MultiValue
    // [1301 - 1400]: Calculation
    // [1401 - 1500]: Component
    //      [[1421 - 1430]]: Component -> AccessTracker
    //      [[1431 - 1440]]: Component -> AccessProfile
    // [1501 - 1600]: FlowNetwork
    // [1601 - 1700]: ComponentInstance

    // general codes for all types
    // the more specific codes are saved with their respective types (i.e. Parameter)
    public enum ParamStructCommonSaveCode : int
    {
        INVALID_CODE = -11, // random, has to be negative (DXF convention)
        ENTITY_START = 0,   // DXF specs
        ENTITY_NAME = 2,    // DXF specs
        COORDS_X = 10,      // DXF specs
        COORDS_Y = 20,      // DXF specs
        CLASS_NAME = 100,   // AutoCAD specs
        ENTITY_GLOBAL_ID = 899, // custom
        ENTITY_LOCAL_ID = 900,    // custom
        NUMBER_OF = 901,    // ...
        TIME_STAMP = 902,
        ENTITY_REF = 903,   // saves the ID of a referenced entity (can be in another file)
        ENTITY_KEY = 904,   // for saving dictionaries

        STRING_VALUE = 909,
        X_VALUE = 910,
        Y_VALUE = 920,
        Z_VALUE = 930,
        W_VALUE = 940,
        V5_VALUE = 950,
        V6_VALUE = 960,
        V7_VALUE = 970,
        V8_VALUE = 980,
        V9_VALUE = 990,
        V10_VALUE = 1000
    }
}
