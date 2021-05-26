using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace CCFlow
{
    class Program
    {
        public static ModuleDefMD asm;
        public static string path;

        static void CleanCflow()
        {
            foreach (TypeDef type in asm.Types)
            {
                foreach (MethodDef methods in type.Methods)
                {
                    if (!methods.HasBody) { continue; }
                    if (!isCflowMethod(methods)) { continue; }
                    methods.Body.KeepOldMaxStack = true;
                    Console.WriteLine();
                    Console.WriteLine("Current method: " + methods.Name);
                    //int cflowcyclecount = methods.Body.Instructions[0].GetLdcI4Value();
                    Local cflowcyclevar = (Local)methods.Body.Instructions[1].Operand;
                    List<Instruction[]> chunks = new List<Instruction[]>();
                    List<int> blockNum = new List<int>();
                    for (int x = 0; x < CountChunks(methods, cflowcyclevar)+1; x++)
                    {
                        if (GetChunk(methods, cflowcyclevar, x).Count() == 0) { continue; }
                        blockNum.Add(GetChunk(methods, cflowcyclevar, x, true)[0].GetLdcI4Value());
                        chunks.Add(GetChunk(methods, cflowcyclevar, x, false));
                    }
                    Console.WriteLine("Saving sorted method...");
                    StreamWriter me_file = File.CreateText("log\\CCFlow_log_" + methods.Name + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString() + ".txt");
                    blockNum.Sort();
                    Array.Sort(blockNum.ToArray(), chunks.ToArray());
                    //chunks.Add(new Instruction[] { OpCodes.Ret.ToInstruction() });
                    //blockNum.Add(int.MaxValue);
                    int tempCount = 0;
                    foreach (Instruction[] inst_arr in chunks)
                    {
                        me_file.WriteLine(methods.Name + " (" +blockNum[chunks.IndexOf(inst_arr)] + "):");
                        foreach (Instruction inst in inst_arr)
                        {
                            methods.Body.Instructions.Insert(tempCount++, inst);
                            me_file.WriteLine(inst.ToString());
                        }
                        me_file.WriteLine();
                    }
                    tempCount--;
                    methods.Body.Instructions.Insert(tempCount, OpCodes.Ldstr.ToInstruction("v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v ORIGINAL CFLOW v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v-v"));
                    methods.Body.Instructions.Insert(tempCount, OpCodes.Ldstr.ToInstruction("----------------------------------------------------------------------------------------------------------------------------------"));
                    methods.Body.Instructions.Insert(tempCount, OpCodes.Ldstr.ToInstruction("----------------------------------------------------------------------------------------------------------------------------------"));
                    methods.Body.Instructions.Insert(tempCount, OpCodes.Ldstr.ToInstruction("----------------------------------------------------------------------------------------------------------------------------------"));
                    methods.Body.Instructions.Insert(tempCount, OpCodes.Ldstr.ToInstruction("^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^  CLEANED CODE  ^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^-^"));
                    me_file.Close();
                }
            }
        }

        static int CountChunks(MethodDef methods, Local local)
        {
            int output = 0;
            foreach (Instruction inst in methods.Body.Instructions)
            {
                if (inst.OpCode.Equals(OpCodes.Stloc))
                {
                    if ((Local)inst.Operand == local)
                    {
                        output++;
                    }
                }
            }
            return output;
        }

        static Instruction[] GetChunk(MethodDef methods, Local local, int index, bool getCflowMatch = false)
        {
            if (CountChunks(methods, local) < index) { throw new OverflowException(); }
            List<Instruction> chunk = new List<Instruction>(); 
            foreach (Instruction inst in methods.Body.Instructions)
            {
                if (inst.OpCode.Equals(OpCodes.Ldloc))
                {
                    if ((Local)inst.Operand == local)
                    {
                        if (methods.Body.Instructions[methods.Body.Instructions.IndexOf(inst) + 1].GetLdcI4Value() == index)
                        {
                            if (getCflowMatch) { chunk.Add(methods.Body.Instructions[methods.Body.Instructions.IndexOf(inst) + 1]); }
                            bool endofChunk = false;
                            for (int x_inst = methods.Body.Instructions.IndexOf(inst) + 4; x_inst < methods.Body.Instructions.Count; x_inst++)
                            {
                                Instruction inst_ = methods.Body.Instructions[x_inst];
                                switch (inst_.OpCode.Code)
                                {
                                    case Code.Ldc_I4:
                                        if (methods.Body.Instructions[x_inst+2].OpCode.Equals(OpCodes.Nop)) { endofChunk = true; }
                                        break;
                                    case Code.Br:
                                        inst_ = (Instruction)inst_.Operand;
                                        x_inst = methods.Body.Instructions.IndexOf(inst_);
                                        break;
                                    case Code.Pop:
                                        continue;
                                }
                                if (endofChunk) { break; }
                                chunk.Add(inst_);
                            }
                        }
                    }
                }
            }
            return chunk.ToArray();
        }

        static bool isCflowMethod(MethodDef methods)
        {
            try
            {
                methods.Body.Instructions[0].GetLdcI4Value();
                Local temp = (Local)methods.Body.Instructions[1].Operand;
                return true;
            }
            catch { }
            return false;
        }

        static void Main(string[] args)
        {
            path = args[0];
            asm = ModuleDefMD.Load(args[0]);
            CleanCflow();
            ModuleWriterOptions moduleWriterOptions = new ModuleWriterOptions(asm);
            moduleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            moduleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            NativeModuleWriterOptions nativeModuleWriterOptions = new NativeModuleWriterOptions(asm, true);
            nativeModuleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            nativeModuleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            if (asm.IsILOnly) { asm.Write(Path.GetFileNameWithoutExtension(path) + "-CCFlow" + Path.GetExtension(path)); }
            else { asm.NativeWrite(Path.GetFileNameWithoutExtension(path) + "-CCFlow" + Path.GetExtension(path)); }
            Console.WriteLine("done");
            Console.ReadKey();
        }
    }
}
