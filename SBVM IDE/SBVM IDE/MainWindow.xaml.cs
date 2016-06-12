using Microsoft.CSharp;
using Microsoft.Win32;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace SBVM_IDE
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private builder builder = new builder();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void debug(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("debug");
            builder.buildProject(textBox.Text, false);
        }

        private void release(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("release");
            builder.buildProject(textBox.Text, true);
        }

    }

    public class builder
    {
        private int i;
        private string converted;
        private string pathOfInterpreter = @"..\interpreter\interpreter\Program.cs";
        private string[] instruction = { "START", "NOP", "PUSH", "POP", "ADD", "SUB", "MUL", "DIV", "CMP", "JMP", "JE", "JNE", "DUP", "SWAP", "PRINT", "READ", "POS", "INC", "DEC", "END" };
        private byte[] opCode = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0xFF };
        private byte[] convertedArray;
        private bool interpreterFound = false;
        private IDictionary<string, byte> opCodeDictionary = new Dictionary<string, byte>();

        public builder()
        {
            interpreterFound = File.Exists(pathOfInterpreter);
            for (i = 0; i < instruction.Length; i++)
            {
                opCodeDictionary.Add(instruction[i], opCode[i]);
            }
        }

        public void buildProject(String code, bool release)
        {

            convertedArray = Encoding.ASCII.GetBytes((code));
            for (int i = 0; i < convertedArray.Length; i++)
            {
                if (convertedArray[i] == 0x0D) // return = ^
                {
                    convertedArray[i] = 0x00;
                    convertedArray[i + 1] = 0x5E;
                    i++;
                }
                if (convertedArray[i] == 0x20) // space = µ
                {
                    convertedArray[i] = 0x3F;
                }
            }
            converted = BitConverter.ToString(convertedArray);
            converted = converted.Replace("-", "");
            Console.WriteLine(converted);

            foreach (var element in opCodeDictionary)
            {
                var instructionInHexString = new { hex = BitConverter.ToString(Encoding.ASCII.GetBytes(element.Key)).Replace("-", "") };
                if (converted.Contains(instructionInHexString.hex))
                {
                    converted = converted.Replace(instructionInHexString.hex, BitConverter.ToString(new byte[] { opCodeDictionary[element.Key] }));
                }
            }

            if (!interpreterFound)
            {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = "Interpreter (*.cs)|*.cs";
                open.ShowDialog();
                pathOfInterpreter = open.FileName;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Executable (*.exe)|*.exe";
            save.ShowDialog();

            string source = File.ReadAllText(pathOfInterpreter);

            if (release)
            {
                source = source.Replace("#define DEBUG", "");
            }

            //https://support.microsoft.com/fr-fr/kb/304655
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            ICodeCompiler icc = codeProvider.CreateCompiler();
            System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = save.FileName;

            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");

            parameters.CompilerOptions = "/optimize";

            System.Resources.ResourceWriter writer = new System.Resources.ResourceWriter("interpreter.Properties.Resources.resources");
            writer.AddResource("ProgramBin", converted);
            writer.Generate();
            writer.Close();
            parameters.EmbeddedResources.Add("interpreter.Properties.Resources.resources");

            CompilerResults results = icc.CompileAssemblyFromSource(parameters, source);
            if (results.Errors.Count > 0)
            {
                String error = "";
                foreach (CompilerError CompErr in results.Errors)
                {
                    error = error + "Line number " + CompErr.Line +
                    ", Error Number: " + CompErr.ErrorNumber +
                    ", '" + CompErr.ErrorText + ";";
                    MessageBox.Show(error);
                }
            }
            else
            {
                MessageBox.Show("ok.");
            }
        }
    }


}