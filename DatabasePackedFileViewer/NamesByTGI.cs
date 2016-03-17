using System;
using DBPF;
using System.Collections.Generic;

namespace DatabasePackedFileViewer
{
    public class NamesByTGI
    {
        internal static Dictionary<TypeGroupInstance, String> NAMES = new Dictionary<TypeGroupInstance, String>();

        static NamesByTGI()
        {
            NAMES.Add(new TypeGroupInstance(0xca16374f, 0, 0), "Network Subfile 2");
            NAMES.Add(new TypeGroupInstance(0xc9c05c6e, 0, 0), "Network Subfile 1");
            NAMES.Add(new TypeGroupInstance(0x6a0f82b2, 0, 0), "Network Index Subfile");
        }

        public static string getNameFor(TypeGroupInstance tgi)
        {
            string name;
            return NAMES.TryGetValue(tgi, out name)
                || NAMES.TryGetValue(new TypeGroupInstance(tgi._typeId, tgi._instanceId, 0), out name)
                || NAMES.TryGetValue(new TypeGroupInstance(tgi._typeId, 0, 0), out name)
                ? name : null;
        }
    }
}
