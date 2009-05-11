﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Magpie.Interpreter
{
    public class Machine
    {
        public event EventHandler<PrintEventArgs> Printed;

        public Machine(IForeignRuntimeInterface foreignInterface)
        {
            mForeignInterface = foreignInterface;
        }

        public void Interpret(Stream stream)
        {
            BytecodeFile bytecode = new BytecodeFile(stream);

            Interpret(bytecode, String.Empty);
        }

        public void Interpret(BytecodeFile file, string argument)
        {
            mFile = file;

            // find "main"
            Push(mFile.OffsetToMain);
            Call(0);

            Interpret();
        }

        public void Interpret(BytecodeFile file)
        {
            Interpret(file, String.Empty);
        }

        public void Interpret()
        {
            bool running = true;

            while (running)
            {
                OpCode theOp = ReadOpCode();
                //Console.WriteLine(theOp.ToString());

                switch (theOp)
                {
                    case OpCode.PushNull:       Push((Structure)null); break;
                    case OpCode.PushBool:       Push(ReadByte() != 0); break;
                    case OpCode.PushInt:        Push(ReadInt()); break;
                    case OpCode.PushString:     Push(mFile.ReadString(ReadInt())); break;

                    case OpCode.PushLocals:     Push(mCurrentFrame); break;

                    case OpCode.Alloc:
                        {
                            int slots = ReadInt();
                            Structure structure = new Structure(slots);

                            // initialize it
                            // note: slots are on stack in reverse order
                            // because they have been pushed in forward order.
                            // this ensures that arguments are evaluated left to right.
                            // for example: calling Foo (1, 2, 3) will create the arg
                            // tuple by evaluating 1, 2, 3 in order. this leaves the
                            // stack (from top down) looking like 3 2 1.
                            for (int i = slots - 1; i >= 0; i--)
                            {
                                structure[i] = Pop();
                            }

                            Push(structure);
                        }
                        break;

                    case OpCode.Load:
                        {
                            byte index = ReadByte();
                            Structure struc = PopStructure();
                            Value op = struc[index];

                            Push(op);
                        }
                        break;

                    case OpCode.Store:
                        {
                            byte index = ReadByte();
                            Structure struc = PopStructure();
                            struc[index] = Pop();
                        }
                        break;

                    case OpCode.LoadArray:
                        {
                            Structure struc = PopStructure();
                            int index = PopInt();

                            //### bob: should bounds-check

                            // add one to skip the first slot which holds the array size
                            Push (struc[index + 1]);
                        }
                        break;

                    case OpCode.StoreArray:
                        {
                            Structure struc = PopStructure();
                            int index = PopInt();
                            Value value = Pop();

                            //### bob: should bounds-check

                            // add one to skip the first slot which holds the array size
                            struc[index + 1] = value;
                        }
                        break;

                    case OpCode.SizeArray:
                        {
                            Structure struc = PopStructure();

                            // array size is the first element
                            Push(struc[0]);
                        }
                        break;

                    case OpCode.Call0: Call(0); break;
                    case OpCode.Call1: Call(1); break;
                    case OpCode.CallN: Call(2); break;

                    case OpCode.ForeignCall0: ForeignCall(0); break;
                    case OpCode.ForeignCall1: ForeignCall(1); break;
                    case OpCode.ForeignCallN: ForeignCall(2); break;

                    case OpCode.Return:
                        {
                            mInstruction = mCurrentFrame[mCurrentFrame.Count - 1].Int;
                            mCurrentFrame = mCurrentFrame[mCurrentFrame.Count - 2].Struct;

                            // stop completely if we've returned from main
                            if (mCurrentFrame == null) running = false;
                        }
                        break;
                        
                    case OpCode.Jump:               mInstruction = ReadInt(); break;
                    case OpCode.JumpIfFalse:        int offset = ReadInt();
                                                    if (!PopBool()) mInstruction = offset;
                                                    break;

                    case OpCode.BoolToString:       Push(PopBool() ? "true" : "false"); break;
                    case OpCode.IntToString:        Push(PopInt().ToString()); break;

                    case OpCode.EqualBool:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Bool == tuple[1].Bool);
                        }
                        break;

                    case OpCode.EqualInt:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Int == tuple[1].Int);
                        }
                        break;

                    case OpCode.EqualString:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].String == tuple[1].String);
                        }
                        break;

                    case OpCode.LessInt:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Int < tuple[1].Int);
                        }
                        break;

                    case OpCode.GreaterInt:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Int > tuple[1].Int);
                        }
                        break;

                    case OpCode.NegateBool:         Push(!PopBool()); break;
                    case OpCode.NegateInt:          Push(-PopInt()); break;

                    case OpCode.AndBool:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Bool && tuple[1].Bool);
                        }
                        break;

                    case OpCode.OrBool:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Bool || tuple[1].Bool);
                        }
                        break;

                    case OpCode.AddInt:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Int + tuple[1].Int);
                        }
                        break;

                    case OpCode.SubInt:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Int - tuple[1].Int);
                        }
                        break;

                    case OpCode.MultInt:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Int * tuple[1].Int);
                        }
                        break;

                    case OpCode.DivInt:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].Int / tuple[1].Int);
                        }
                        break;

                    case OpCode.HasValue:
                        Push(PopStructure() != null);
                        break;

                    case OpCode.BoxValue:
                        {
                            Structure structure = new Structure(1);
                            structure[0] = Pop();
                            Push(structure);
                        }
                        break;

                    case OpCode.UnboxValue:
                        Push(PopStructure()[0]);
                        break;

                    case OpCode.AddString:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].String + tuple[1].String);
                        }
                        break;

                    case OpCode.Print:
                        string text = PopString();
                        if (Printed != null) Printed(this, new PrintEventArgs(text));
                        break;

                    case OpCode.StringSize:         Push(PopString().Length); break;

                    case OpCode.Substring:
                        {
                            Structure tuple = PopStructure();
                            Push(tuple[0].String.Substring(tuple[1].Int, tuple[2].Int));
                        }
                        break;

                    default: throw new Exception("Unknown opcode.");
                }
            }
        }

        private OpCode ReadOpCode()
        {
            return (OpCode)mFile.Bytes[mInstruction++];
        }

        private byte ReadByte()
        {
            return mFile.Bytes[mInstruction++];
        }

        private char ReadChar()
        {
            int c = ((int)mFile.Bytes[mInstruction++]) |
                    ((int)mFile.Bytes[mInstruction++] << 8);

            return (char)c;
        }

        private int ReadInt()
        {
            return ((int)mFile.Bytes[mInstruction++]) |
                   ((int)mFile.Bytes[mInstruction++] << 8) |
                   ((int)mFile.Bytes[mInstruction++] << 16) |
                   ((int)mFile.Bytes[mInstruction++] << 24);
        }

        private void Push(Value value) { mOperands.Push(value); }
        private void Push(bool value) { Push(new Value(value)); }
        private void Push(char value) { Push(new Value(value)); }
        private void Push(int value) { Push(new Value(value)); }
        private void Push(string value) { Push(new Value(value)); }
        private void Push(Structure value) { Push(new Value(value)); }

        private Value Pop() { return mOperands.Pop(); }
        private bool PopBool() { return mOperands.Pop().Bool; }
        private int PopInt() { return mOperands.Pop().Int; }
        private string PopString() { return mOperands.Pop().String; }
        private Structure PopStructure() { return mOperands.Pop().Struct; }

        private Structure MakeCallFrame(int numLocals, Structure parentFrame, int instruction)
        {
            var frame = new Structure(numLocals + 2);
            frame[numLocals] = new Value(parentFrame);
            frame[numLocals + 1] = new Value(instruction);

            return frame;
        }

        private void Call(int paramType)
        {
            // jump to the function
            int previousInstruction = mInstruction;

            mInstruction = PopInt();
            int numLocals = ReadInt();

            mCurrentFrame = MakeCallFrame(numLocals, mCurrentFrame, previousInstruction);

            // pop and store the argument
            if (paramType != 0) mCurrentFrame[0] = Pop();
        }

        private void ForeignCall(int paramType) // (int id, Value[] args)
        {
            int id = ReadInt();

            Value[] args;
            switch (paramType)
            {
                case 0: args = new Value[0]; break;
                case 1: args = new Value[] { Pop() }; break;
                case 2: args = PopStructure().Fields.ToArray(); break;
                default: throw new ArgumentException("Unknown parameter type.");
            }

            Value result = mForeignInterface.ForeignCall(id, args);
            if (result != null) Push(result);
        }

        private BytecodeFile mFile;

        //### bob: could also just store this in the current frame
        private int mInstruction; // position in bytecode file

        private readonly Stack<Value> mOperands = new Stack<Value>();

        // call frame for the current function
        // contains:
        // 0 ... n: local variables
        // n + 1  : reference to parent call frame
        // n + 2  : instruction pointer for parent frame
        private Structure mCurrentFrame;

        private readonly IForeignRuntimeInterface mForeignInterface;
    }
}