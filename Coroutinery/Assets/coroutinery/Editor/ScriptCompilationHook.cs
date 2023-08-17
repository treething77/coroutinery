using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using Debug = UnityEngine.Debug;

namespace aeric.coroutinery {

    [InitializeOnLoad]
    public class ScriptCompilationHook : UnityEditor.AssetModificationProcessor {
        static ScriptCompilationHook() {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] arg2)
        { 
            
            //Load the asset that contains the coroutine debug info
            //Look at our default path
            string defaultPath = "Assets/coroutinery/Debug/";
            string filename = "coroutine_debug_info.asset";
            string fullPath = defaultPath + filename;
            CoroutineDebugInfo debugAsset = null;
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                debugAsset = AssetDatabase.LoadAssetAtPath<CoroutineDebugInfo>(fullPath);
                if (debugAsset == null)
                {
                    //Maybe it got moved?
                    string[] debugAssets = AssetDatabase.FindAssets("t:CoroutineDebugInfo");
                    if (debugAssets.Length == 0)
                    {
                        //No asset so lets create one
                        debugAsset = ScriptableObject.CreateInstance<CoroutineDebugInfo>();
                        AssetDatabase.CreateAsset(debugAsset, fullPath);
                    }
                    else
                    {
                        //If we have more than one just take the first one
                        if (debugAssets.Length > 1)
                        {
                            Debug.LogWarning("Multiple CoroutineDebugInfo assets found:");
                            foreach (var d in debugAssets)
                            {
                                Debug.LogWarning(d);
                            }
                        }

                        fullPath = debugAssets[0];
                        debugAsset = AssetDatabase.LoadAssetAtPath<CoroutineDebugInfo>(fullPath);
                    }
                }

                if (debugAsset == null)
                {
                    //we failed to find or create the asset
                    Debug.LogError("Failed to find or create CoroutineDebugInfo asset");
                    return;
                }

                //clear out any existing data, we are going to recreate it entirely
                debugAsset.ClearData();
                
                stopwatch.Stop();
                Debug.Log($"Time loading debug asset: {stopwatch.ElapsedMilliseconds} ms");

            }
            //  Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            //foreach (var assembly in assemblies) {
            bool processAssembly = assemblyPath.Contains("ScriptAssemblies");
            
            // if (assembly.IsDynamic) continue;
            if (assemblyPath.Contains("Unity")) processAssembly = false;

            if (processAssembly)
            {
                Debug.Log($"Scanning assembly: {assemblyPath}");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                //Use Cecil to load the assembly definition
                AssemblyDefinition assemblyDef;
                TypeCache.TypeCollection enumeratorTypes;
                IEnumerable<TypeDefinition> typeDefs;
          

                //Use System.Reflection to get the actual types
            //    Assembly a;

                {
                    Stopwatch stopwatch2 = new Stopwatch();
                    stopwatch2.Start();
                    
                    assemblyDef =
                        AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadSymbols = true, ReadWrite = false});
                    typeDefs = assemblyDef.MainModule.GetTypes();
                    
                    stopwatch2.Stop();
                    Debug.Log($"Time loading type defs: {stopwatch2.ElapsedMilliseconds} ms");
                    
                    stopwatch2 = new Stopwatch();
                    stopwatch2.Start();

             //       a= System.Reflection.Assembly.LoadFrom(assemblyPath);
                    enumeratorTypes = TypeCache.GetTypesDerivedFrom<IEnumerator>();

                    stopwatch2.Stop();
                    Debug.Log($"Time loading types: {stopwatch2.ElapsedMilliseconds} ms");
                    
                }

              //  Type[] types = a.GetTypes();

             //   var generatedMethods = TypeCache.GetMethodsWithAttribute<CompilerGeneratedAttribute>();

                foreach (var type in enumeratorTypes)
                {
                    if (!type.Assembly.Location.Contains("ScriptAssemblies")) continue;
                    if (type.Assembly.FullName != assemblyDef.FullName) continue;
                    
                    if (type.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                    {
                        CoroutineSourceMapping sourceMapping = new CoroutineSourceMapping();
                        sourceMapping.typeName = type.Name;
                        sourceMapping.typeNamespace = type.Namespace;

                        List<int> sourcePts = new List<int>();
                        //names don't match
                        //one uses '/' as a split the other uses '+'

                        string fixedClassName = type.FullName.Replace("+<", "/<");
                        //   Debug.Log($"Coroutine class: {fixedClassName}");

                        var coroutineTypeDef = typeDefs.First(x => x.FullName == fixedClassName);
                        
                        
                        //TODO: error if null

                        //TODO: replace the loop with a find
                        // dont error if we dont find it
                        foreach (MethodDefinition m in coroutineTypeDef.Methods)
                        {
                            if (m.Name.Contains("MoveNext"))
                            {
                                //Debug.Log("MoveNsdsdfext");
                                Collection<Instruction> instructions = m.Body.Instructions;

                                sourceMapping.sourceUrl = m.DebugInformation.SequencePoints[0].Document.Url;

                                Instruction stateSwitch =
                                    instructions.FirstOrDefault(i => i.OpCode.Code == Code.Switch);
                                if (stateSwitch == null)
                                {
                                    continue;
                                }
                                //TODO: error if null

                                // Debug.Log($"State switch at {stateSwitch.Offset}");

                                //for each state jump instruction, get first instruction after that with value 
                                //sequence point with a valid start line
                                Instruction[] jumpInstructions = (Instruction[])stateSwitch.Operand;

                                foreach (var jumpInstr in jumpInstructions)
                                {

                                    //where are we jumping to?
                                    Instruction destInstr = (Instruction)jumpInstr.Operand;
                                    if (destInstr == null) continue;

                                    SequencePoint destSeqPt = null;
                                    do
                                    {
                                        destSeqPt = m.DebugInformation.GetSequencePoint(destInstr);
                                        //TODO: temp constant to skip invalid lines
                                        if (destSeqPt != null && destSeqPt.StartLine < 100000)
                                        {

                                            // Debug.Log(destSeqPt.Document.Url);
                                            //   Debug.Log(destSeqPt.StartLine);

                                            //TODO: dont want the full path, just relative to project

                                            //TODO: If we have a URL from the sequence point then override the one we have because this will be more accurate
                                            sourceMapping.sourceUrl = destSeqPt.Document.Url;

                                            sourcePts.Add(destSeqPt.StartLine);
                                        }
                                        else
                                        {
                                            destSeqPt = null;
                                            destInstr = destInstr.Next;
                                        }

                                    } while (destSeqPt == null);
                                }
                            }
                        }

                        sourceMapping.stateSourcePoints = sourcePts.ToArray();
                        debugAsset.AddSourceMapping(sourceMapping);
                    }
                }
                //   }

                EditorUtility.SetDirty(debugAsset);

                // Stop stopwatch
                stopwatch.Stop();
                Debug.Log($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
            }

        }

        private static void OnCompilationFinished(object obj)
        {
  
        }
    }
}