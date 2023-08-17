using System;
using System.Collections.Generic;
using UnityEngine;

namespace aeric.coroutinery {
    
    //TODO: move to own file
    [Serializable]
    public class CoroutineSourceMapping {
        public string sourceUrl;
        public string typeName;
        public string typeNamespace;

        public int[] stateSourcePoints;
    }
    
    public class CoroutineDebugInfo : ScriptableObject {
        public void AddSourceMapping(CoroutineSourceMapping mapping) {
            sourceMapping.Add(mapping);
        }

        public void ClearData() {
            sourceMapping.Clear();
        }

        public List<CoroutineSourceMapping> sourceMapping = new List<CoroutineSourceMapping>();
    }
}