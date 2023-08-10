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
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace aeric.coroutinery {
    
    public class ScriptCompilationHook  {
        
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded() {
            //profile this whole thing
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            string projectFolder = Path.Combine( Application.dataPath, "../Library" );

            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic) continue;
                if (!assembly.Location.Contains("ScriptAssemblies")) continue;
                if (assembly.Location.Contains("Unity")) continue;
                Debug.Log($"Scanning assssembly: {assembly.Location}");

                AssemblyDefinition assemblyDef = AssemblyDefinition.ReadAssembly(assembly.Location, new ReaderParameters { ReadSymbols = true });
                IEnumerable<TypeDefinition> typeDefs = assemblyDef.MainModule.GetTypes();

                
                Type[] types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    if (typeof(IEnumerator).IsAssignableFrom(type) &&
                        type.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                    {
                      //  Debug.Log($"Coroutine dclass: {type.Name}");

                        //names don't match
                        //one uses '/' as a split the other uses '+'

                        string ff = type.FullName.Replace("+<", "/<");
                        Debug.Log($"Coroutine dclass: {ff}");

                        var coroutineTypeDef = typeDefs.First(x => x.FullName == ff);
                        
                        foreach (var m in coroutineTypeDef.Methods) {
                            if (m.Name.Contains("MoveNext")) {
                                Debug.Log("MoveNext");
                                Collection<Instruction> instructions = m.Body.Instructions;

                                Instruction stateSwitch = instructions.First(i => i.OpCode.Code == Code.Switch);

                                Debug.Log($"State switch at {stateSwitch.Offset}");
                                
                                //for each state jump instruction, get first instruction after that with value 
                                //sequence point with a valid start line
                                Instruction[] jumpInstructions = (Instruction[])stateSwitch.Operand;

                                foreach (var jumpInstr in jumpInstructions) {
                            
                                    //where are we jumping to?
                                    Instruction destInstr = (Instruction)jumpInstr.Operand;
                                    SequencePoint destSeqPt = null;
                                    do {
                                        destSeqPt = m.DebugInformation.GetSequencePoint(destInstr);
                                        //TODO: temp constant
                                        if (destSeqPt != null && destSeqPt.StartLine < 1000) {
                                    
                                            Debug.Log(destSeqPt.Document.Url);
                                            Debug.Log(destSeqPt.StartLine);

                                           // sourceFile = destSeqPt.Document.Url;
                                            //linePerState.Add(destSeqPt.StartLine);
                                        }
                                        else {
                                            destSeqPt = null;
                                            destInstr = destInstr.Next;
                                        }
                                
                                    } while (destSeqPt == null);
                            
                                }

                            }
                        }
                    }
                }
            }
            
            // Stop stopwatch
            stopwatch.Stop();
            Debug.Log($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
