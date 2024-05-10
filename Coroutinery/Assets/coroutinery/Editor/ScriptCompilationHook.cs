using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using Debug = UnityEngine.Debug;

namespace aeric.coroutinery
{
    /// <summary>
    /// Hooks into script compilation to build source mappings for coroutines.
    /// Can be triggered manually via menu item or automatically on assembly compilation.
    /// </summary>
    [InitializeOnLoad]
    public class ScriptCompilationHook
    {
        static ScriptCompilationHook()
        {
            //Register for script compilation events
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        //Rebuild the source mappings for all user script assemblies
        [MenuItem("Tools/Coroutinery/Rebuild Source Mappings")]
        public static void RebuildAll()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.IsDynamic) continue;
                //This method will filter out all assemblies that are not user script assemblies
                BuildAssemblySourceMapping(assembly.Location);
            }
        }

        //add menu item to toggle stack trace capture
        [MenuItem("Tools/Coroutinery/Stack Traces")]
        public static void ToggleStackCollection()
        {
            bool collectStacks = EditorPrefs.GetBool("Coroutinery/Stack Traces", false);
            collectStacks = !collectStacks;
            EditorPrefs.SetBool("Coroutinery/Stack Traces", collectStacks);
            Debug.Log($"Coroutinery: Collect stacks: {collectStacks}");
        }

        //validate menu item
        [MenuItem("Tools/Coroutinery/Stack Traces", true)]
        public static bool TogglStackCollectionValidate()
        {
            bool collectStacks = EditorPrefs.GetBool("Coroutinery/Stack Traces", false);
            Menu.SetChecked("Coroutinery/Stack Traces", collectStacks);
            return true;
        }

        //add menu item to set auto build mappings
        [MenuItem("Tools/Coroutinery/Auto build source mappings")]
        public static void ToggleAutoBuildMappings()
        {
            bool autoBuildMappings = EditorPrefs.GetBool("Coroutinery/Auto build source mappings", true);
            autoBuildMappings = !autoBuildMappings;
            EditorPrefs.SetBool("Coroutinery/Auto build source mappings", autoBuildMappings);
            Menu.SetChecked("Coroutinery/Auto build source mappings", autoBuildMappings);
            Debug.Log($"Coroutinery: Auto build source mappings: {autoBuildMappings}");
        }

        //validate menu item
        [MenuItem("Tools/Coroutinery/Auto build source mappings", true)]
        public static bool ToggleAutoBuildMappingsValidate()
        {
            bool autoBuildMappings = EditorPrefs.GetBool("Coroutinery/Auto build source mappings", true);
            Menu.SetChecked("Coroutinery/Auto build source mappings", autoBuildMappings);
            return true;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] arg2)
        {
            //If auto build mappings is enabled, build the mappings for this assembly
            bool autoBuildMappings = EditorPrefs.GetBool("Coroutinery/Auto build source mappings", true);
            if (autoBuildMappings)
            {
                BuildAssemblySourceMapping(assemblyPath);
            }
        }

        //Determine if this is a user script assembly that we should capture coroutines from
        private static bool IsUserScriptAssembly(string assemblyPath)
        {
            if (!assemblyPath.Contains("ScriptAssemblies")) return false;
            if (assemblyPath.Contains("Unity")) return false;
            return true;
        }

        private static void BuildAssemblySourceMapping(string assemblyPath)
        {
            if (!IsUserScriptAssembly(assemblyPath)) return;

            CoroutineDebugInfo debugInfoAsset = LoadCoroutineDebugAsset();
            if (debugInfoAsset == null)
            {
                Debug.LogError("Coroutinery: Could not load CoroutineDebugInfo asset");
                return;
            }

            LogCompilationInfo($"Processing assembly: {assemblyPath}");

            //Time the overal process 
            Stopwatch overallTimer = new Stopwatch();
            overallTimer.Start();

            //Use Cecil to load the assembly definition
            AssemblyDefinition assemblyDef;
            IEnumerable<TypeDefinition> typeDefs;
            List<Type> enumeratorTypes = new List<Type>();

            /////////////////////////////////////////////////////////////////////////////////////////
            //Load the assembly definition, and time it
            Stopwatch assemblyLoadTimer = new Stopwatch();
            assemblyLoadTimer.Start();

            assemblyDef = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadSymbols = true, ReadWrite = false });
            typeDefs = assemblyDef.MainModule.GetTypes();

            assemblyLoadTimer.Stop();
            LogCompilationInfo($"Time loading type defs: {assemblyLoadTimer.ElapsedMilliseconds} ms");
            /////////////////////////////////////////////////////////////////////////////////////////

            //Get the asset for this assembly and clear out its data prior to replacing it
            CoroutineAssemblySourceMappings assemblyDebugAsset = debugInfoAsset.GetAssemblySourceMapping(assemblyDef.Name.Name);
            assemblyDebugAsset.ClearData();

            foreach (TypeDefinition typeDef in typeDefs)
            {
                //to capture coroutines we first look for compiler-generated types
                if (!typeDef.HasCustomAttributes) continue;
                //does it have the CompilerGeneratedAttribute 
                bool isCompilerGenerated = typeDef.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
                if (!isCompilerGenerated) continue;

                //then we look for the IEnumerator interface
                bool isEnumerator = typeDef.Interfaces.Any(i => i.InterfaceType.FullName == "System.Collections.IEnumerator");
                if (!isEnumerator) continue;

                CoroutineSourceMapping sourceMapping = new CoroutineSourceMapping();
                sourceMapping.typeName = typeDef.Name;
                sourceMapping.outerTypeName = typeDef.DeclaringType.Name;
                sourceMapping.typeNamespace = typeDef.DeclaringType.Namespace;

                List<int> sourcePts = new List<int>();

                foreach (MethodDefinition m in typeDef.Methods)
                {
                    //We are looking for MoveNext methods that have debug information
                    if (!m.Name.Contains("MoveNext")) continue;
                    if (!m.HasBody) continue;
                    if (m.DebugInformation == null) continue;
                    if (!m.DebugInformation.HasSequencePoints) continue;

                    Collection<Instruction> instructions = m.Body.Instructions;

                    //Get the relative path of the source file
                    string fullUrl = m.DebugInformation.SequencePoints[0].Document.Url;
                    int assetsFolderIndex = fullUrl.IndexOf("Assets");
                    //If the source file is not within the Assets folder then take the full url
                    if (assetsFolderIndex == -1) {
                        assetsFolderIndex = 0;
                    }

                    string url = fullUrl.Substring(assetsFolderIndex);
                    sourceMapping.sourceUrl = url;

                    //Most coroutines will have a switch on the current state
                    Instruction stateSwitch = instructions.FirstOrDefault(i => (i.OpCode.Code == Code.Switch));
                    if (stateSwitch != null)
                    {
                        //for each state jump instruction, get first instruction after that with value 
                        //sequence point with a valid start line
                        Instruction[] jumpInstructions = (Instruction[])stateSwitch.Operand;

                        foreach (var jumpInstr in jumpInstructions)
                        {
                            //where are we jumping to?
                            Instruction destInstr = (Instruction)jumpInstr.Operand;
                            if (destInstr == null) continue;

                            //get the sequence point for the destination instruction
                            AddSourceMappingPoint(destInstr, m.DebugInformation, sourcePts);
                        }
                    }
                    else
                    {
                        //Simple coroutines might not have a switch on the current state
                        //in that case we just use the first instruction after the yield return
                        Instruction yieldReturnInstr = instructions.FirstOrDefault(i =>
                                                                i.OpCode.Code == Code.Ldc_I4_1 &&
                                                                i.Next != null &&
                                                                i.Next.OpCode.Code == Code.Ret);
                        if (yieldReturnInstr == null)
                            continue;

                        //get the sequence point for the destination instruction
                        AddSourceMappingPoint(yieldReturnInstr, m.DebugInformation, sourcePts);
                    }
                }

                sourceMapping.stateSourcePoints = sourcePts.ToArray();
                assemblyDebugAsset.AddSourceMapping(sourceMapping);
            }

            EditorUtility.SetDirty(debugInfoAsset);

            // Stop stopwatch
            overallTimer.Stop();
            LogCompilationInfo($"Elapsed time: {overallTimer.ElapsedMilliseconds} ms");
        }

        private static void LogCompilationInfo(string msg)
        {
            Debug.Log(msg);
        }
        private static void AddSourceMappingPoint(Instruction destInstr, MethodDebugInformation debugInformation, List<int> sourcePts)
        {
            SequencePoint destSeqPt;
            do
            {
                destSeqPt = debugInformation.GetSequencePoint(destInstr);
                if (destSeqPt != null && destSeqPt.StartLine < 0xFEEFEE)
                {
                    sourcePts.Add(destSeqPt.StartLine);
                }
                else
                {
                    destSeqPt = null;
                    destInstr = destInstr.Next;
                }
            } while (destSeqPt == null);
        }

        private static CoroutineDebugInfo LoadCoroutineDebugAsset()
        {
            //Load the asset that contains the coroutine debug info
            //Look at our default path
            string baseFolderPath = AssetDatabase.GUIDToAssetPath(CoroutineManager.BaseFolderGUID);
            string defaultPath = baseFolderPath + "/Debug/";

            string filename = "coroutine_debug_info.asset";
            string fullPath = defaultPath + filename;
            CoroutineDebugInfo debugAsset = AssetDatabase.LoadAssetAtPath<CoroutineDebugInfo>(fullPath);
            if (debugAsset == null)
            {
                //Maybe it got moved, so do the more expensive search
                string[] debugAssets = AssetDatabase.FindAssets("t:CoroutineDebugInfo");
                if (debugAssets.Length == 0)
                {
                    Debug.LogWarning("No CoroutineDebugInfo was found, creating a new one");

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
            }

            return debugAsset;
        }

        private static void OnCompilationFinished(object obj)
        {
        }
    }
}