using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBPF
{
    public class TGIObjects<T> : Dictionary<UInt32, Dictionary<UInt32, Dictionary<UInt32, T>>> where T : HasTGI
    {
        public T this[TypeGroupInstance key]
        {
            get
            {
                return this[key._typeId][key._groupId][key._instanceId];
            }
        }

        public TGIObjects() : base()
        {
        }

        public TGIObjects(IEnumerable<T> collection) : this()
        {
            foreach (T tgiObj in collection)
            {
                TypeGroupInstance tgi = tgiObj.tgi;
                Dictionary<UInt32, Dictionary<UInt32, T>> groupToInstances;
                if (!TryGetValue(tgi._typeId, out groupToInstances))
                    Add(tgi._typeId, groupToInstances = new Dictionary<uint, Dictionary<uint, T>>());
                Dictionary<UInt32, T> instances;
                if (!groupToInstances.TryGetValue(tgi._groupId, out instances))
                    groupToInstances.Add(tgi._groupId, instances = new Dictionary<uint, T>());
                instances.Add(tgi._instanceId, tgiObj);
            }

        }

        public bool TryGetValue(TypeGroupInstance key, out T val)
        {
            Dictionary<UInt32, Dictionary<UInt32, T>> m1;
            Dictionary<UInt32, T> m2;
            if (TryGetValue(key._typeId, out m1) && m1.TryGetValue(key._groupId, out m2) && m2.TryGetValue(key._instanceId, out val))
                return true;
            val = default(T);
            return false;
        }
    }
}
