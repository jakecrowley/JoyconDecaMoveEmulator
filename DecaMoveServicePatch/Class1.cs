using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static DecaMoveServicePatch.DecaMoveServicePatch;

namespace DecaMoveServicePatch
{
    public class DecaMoveServicePatch
    {
        [DllExport]
        public static void CS_DllMain()
        {
            MessageBox.Show("Bonjour from .NET Framework 4 :O");
            // Don't forget to run almost everything in a thread (avoid ui/thread blocking) !
        }

    }
}
