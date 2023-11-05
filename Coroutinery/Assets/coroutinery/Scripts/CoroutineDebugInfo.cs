using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace aeric.coroutinery
{
    /// <summary>
    /// Contains the source mapping for a single coroutine type.
    /// </summary>
    [Serializable]
    public class CoroutineSourceMapping
    {
        public string sourceUrl;
        public string typeName;
        public string typeNamespace;

        public int[] stateSourcePoints;
        public string outerTypeName;
    }

    /// <summary>
    /// Contains the source info for a single coroutine.
    /// </summary>
    public class SourceInfo
    {
        public string url;
        public int lineNumber;
        public string enumeratorTypeName;
        public string outerTypeName;
    }

    /// <summary>
    /// Acts as a container for source mapping. Has a helper method to get a source info object for a given coroutine enumerator in a given state.
    /// </summary>
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

        internal SourceInfo GetSourceInfo(IEnumerator coroutineEnumerator, int stateValue)
        {
            Type enumeratorType = coroutineEnumerator.GetType();
            CoroutineSourceMapping typeMapping = sourceMapping.Find((x) =>
            {
                return x.typeName == enumeratorType.Name && x.typeNamespace == enumeratorType.Namespace;
            });

            if (typeMapping == null)
            {
                Debug.Log("No source mapping found for type " + enumeratorType.Name + " in namespace " + enumeratorType.Namespace);
                return null;
            }

            if (stateValue >= typeMapping.stateSourcePoints.Length)
            {
                stateValue = 0;
                Debug.Log("State value " + stateValue + " is out of range for type " + enumeratorType.Name + " in namespace " + enumeratorType.Namespace);
            }

            int sourcePoint = typeMapping.stateSourcePoints[stateValue];

            SourceInfo sourceInfo = new SourceInfo();
            sourceInfo.url = typeMapping.sourceUrl;
            sourceInfo.lineNumber = sourcePoint;
            sourceInfo.enumeratorTypeName = enumeratorType.Name;
            sourceInfo.outerTypeName = typeMapping.outerTypeName;

            return sourceInfo;
        }
    }


    /// <summary>
    /// Contains the source mappings generated for all assemblies in the project.
    /// </summary>
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