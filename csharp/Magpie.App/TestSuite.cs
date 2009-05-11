﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Magpie.Compilation;
using Magpie.Interpreter;

namespace Magpie.App
{
    public class TestSuite
    {
        public TestSuite(string testDir)
        {
            mTestDir = testDir;
        }

        public void Run()
        {
            int passed = 0;
            int failed = 0;

            foreach (string test in Directory.GetFiles(mTestDir, "*.mag", SearchOption.AllDirectories))
            {
                //### bob: testing
                //if (!test.EndsWith("IfThen.mag")) continue;

                if (Debugger.IsAttached)
                {
                    // don't catch exceptions when a debugger is attached, that way
                    // the debugger will break at the place where the exception was
                    // thrown and we can see what's going on.
                    RunTest(test);
                }
                else
                {
                    // no debugger, so catch all exceptions
                    try
                    {
                        RunTest(test);
                    }
                    catch (Exception ex)
                    {
                        mErrors.Add("interpreter threw \"" + ex.Message + "\"");
                    }
                }

                string relativePath = test.Replace(mTestDir, "");
                if (mErrors.Count == 0)
                {
                    Console.WriteLine("  pass " + relativePath);
                    passed++;
                }
                else
                {
                    Console.WriteLine("! FAIL " + relativePath);
                    failed++;

                    foreach (string error in mErrors)
                    {
                        Console.WriteLine("       " + error);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("passed {0}/{1} tests", passed, passed + failed);
        }

        private void RunTest(string test)
        {
            mErrors = new List<string>();

            ParseExpected(test);
            IList<CompileError> compileErrors = Script.Run(test, TestPrint);

            if (compileErrors.Count == 0)
            {
                while (mExpectedOutput.Count > 0)
                {
                    mErrors.Add("out of output while expecting \"" + mExpectedOutput.Dequeue() + "\"");
                }
            }
            else
            {
                foreach (var error in compileErrors)
                {
                    if (mExpectedErrorLines.Count > 0)
                    {
                        int expectedLine = mExpectedErrorLines.Dequeue();
                        if (error.Line != expectedLine) mErrors.Add("Expected compile error on line " + expectedLine + " but was on line " + error.Line);
                    }
                    else
                    {
                        mErrors.Add("Got compile error on line " + error.Line + " when no more were expected.");
                    }
                }
            }
        }

        private void ParseExpected(string path)
        {
            mExpectedOutput.Clear();
            mExpectedErrorLines.Clear();

            const string ExpectedHeader = "// expected: ";
            const string ErrorHeader    = "// error line: ";

            foreach (string line in File.ReadAllLines(path))
            {
                if (line.StartsWith(ExpectedHeader))
                {
                    mExpectedOutput.Enqueue(line.Substring(ExpectedHeader.Length));
                }
                else if (line.StartsWith(ErrorHeader))
                {
                    string errorLine = line.Substring(ErrorHeader.Length).Trim();
                    mExpectedErrorLines.Enqueue(Int32.Parse(errorLine));
                }
            }
        }

        private void TestPrint(string text)
        {
            if (mExpectedOutput.Count == 0)
            {
                mErrors.Add("got \"" + text + "\" when not expecting any more output");
            }
            else
            {
                string expecting = mExpectedOutput.Dequeue();

                if (expecting != text)
                {
                    mErrors.Add("got \"" + text + "\" when expecting \"" + expecting + "\"");
                }
            }
        }

        private string mTestDir;
        private Queue<string> mExpectedOutput = new Queue<string>();
        private Queue<int> mExpectedErrorLines = new Queue<int>();
        private List<string> mErrors;
    }
}