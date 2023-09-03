using System;
using System.Collections;
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
        public string outerTypeName;
    }

    public class SourceInfo
    {
        public string url;
        public int lineNumber;
        public string enumeratorTypeName;
        public string outerTypeName;
    }

    //TODO: move to own file
    [Serializable]
    public class CoroutineAssemblySourceMappings
    {
        public string assemblyName;
        public List<CoroutineSourceMapping> sourceMapping = new List<CoroutineSourceMapping>();

        public void AddSourceMapping(CoroutineSourceMapping mapping)
        {
            sourceMapping.Add(mapping);
        }

        public void ClearData()
        {
            sourceMapping.Clear();
        }

        internal SourceInfo GetSourceInfo(IEnumerator c, int stateValue)
        {
            Type type = c.GetType();
            CoroutineSourceMapping typeMapping = sourceMapping.Find((x) =>
            {
                return x.typeName == type.Name && x.typeNamespace == type.Namespace;
            });

            if (typeMapping == null)
            {
                // return "No source mapping found for type " + type.Name + " in namespace " + type.Namespace;
                return null;
            }

            if (stateValue >= typeMapping.stateSourcePoints.Length)
            {
                stateValue = 0;
               // return "State value " + stateValue + " is out of range for type " + type.Name + " in namespace " + type.Namespace;
            }

            int sourcePoint = typeMapping.stateSourcePoints[stateValue];
            //return a tuple of the url and line number

            SourceInfo sourceInfo = new SourceInfo();
            sourceInfo.url = typeMapping.sourceUrl;
            sourceInfo.lineNumber = sourcePoint;
            sourceInfo.enumeratorTypeName = type.Name;
            sourceInfo.outerTypeName = typeMapping.outerTypeName;

            return sourceInfo; 
        }
    }


    public class CoroutineDebugInfo : ScriptableObject 
    {
        //Get mapping from assembly name to source mapping
        public CoroutineAssemblySourceMappings GetAssemblySourceMapping(string assemblyName)
        {
            CoroutineAssemblySourceMappings mapping = assemblyMappings.Find((x) =>
            {
                return x.assemblyName == assemblyName;
            });

            if (mapping == null)
            {
                mapping = new CoroutineAssemblySourceMappings();
                mapping.assemblyName = assemblyName;
                assemblyMappings.Add(mapping);
            }

            return mapping;
        }

        internal SourceInfo GetSourceInfo(IEnumerator c, int stateValue)
        {
            //look in all assemblies for a mapping for this type
            foreach (CoroutineAssemblySourceMappings mapping in assemblyMappings)
            {
                SourceInfo sourceInfo = mapping.GetSourceInfo(c, stateValue);
                if (sourceInfo != null)
                {
                    return sourceInfo;
                }
            }
            return null;
        }

        public List<CoroutineAssemblySourceMappings> assemblyMappings = new List<CoroutineAssemblySourceMappings>();
    }
}