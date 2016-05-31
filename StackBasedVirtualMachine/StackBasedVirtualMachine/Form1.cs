using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{

    public partial class Form1 : Form
    {
        public moveCursorHandler moveCursor;

        public popListBoxHandler popStack;

        public printStackHandler printStack;

        public pushListBoxHandler pushStack;

        public ManualResetEvent threadPauseResume = new ManualResetEvent(true);

        private Thread vmThread;

        public Form1()
        {
            printStack = new printStackHandler(printTextarea);
            pushStack = new pushListBoxHandler(pushListBox);
            popStack = new popListBoxHandler(popListBox);
            moveCursor = new moveCursorHandler(moveCursorTo);
            InitializeComponent();
        }

        public delegate void moveCursorHandler(int Position);

        public delegate void popListBoxHandler();

        public delegate void printStackHandler();

        public delegate void pushListBoxHandler(object element);
        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = imageList1.Images[0];
            vmThread = new Thread(() =>
            {
                threadPauseResume.WaitOne(Timeout.Infinite);
                vm virtualMachine = new vm();
                virtualMachine.justDoIt(textBox1.Text, this);
            });
            vmThread.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            threadPauseResume.Reset();
            pictureBox1.Image = imageList1.Images[1];
        }

        private void button3_Click(object sender, EventArgs e)
        {
            threadPauseResume.Set();
            pictureBox1.Image = imageList1.Images[0];
        }

        private void button4_Click(object sender, EventArgs e)
        {
            builder builder = new builder(this);
            builder.buildProject(textBox1.Text);
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItems.Count == 1)
            {
                String currentStack = listBox1.SelectedItem.ToString();
                listBox1.Items[listBox1.SelectedIndex] = Microsoft.VisualBasic.Interaction.InputBox("READ", "READ", currentStack);
            }
        }

        private void moveCursorTo(int Position)
        {
            pictureBox1.Location = new Point(10, (Position * 15) + 3); ;
        }

        private void popListBox()
        {
            listBox1.Items.RemoveAt(0);
        }

        private void printTextarea()
        {
            textBox2.AppendText(listBox1.Items[0].ToString() + "\r\n");
        }

        private void pushListBox(object element)
        {
            listBox1.Items.Insert(0, element);
        }
    }

    public class builder
    {
        private string converted;
        private byte[] convertedArray;
        private int i;
        private string[] instruction = { "START", "NOP", "PUSH", "POP", "ADD", "SUB", "MUL", "DIV", "CMP", "JMP", "JE", "JNE", "DUP", "SWAP", "PRINT", "READ", "END" };
        private byte[] opCode = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0xFF };
        private IDictionary<string, byte> opCodeDictionary = new Dictionary<string, byte>();
        private Form1 ui;
        public builder(Form1 userInterface)
        {
            this.ui = userInterface;
            for (i = 0; i < instruction.Length; i++)
            {
                opCodeDictionary.Add(instruction[i], opCode[i]);
            }
        }

        public void buildProject(String code)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Interpreter (*.cs)|*.cs";
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Executable (*.exe)|*.exe";

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

            open.ShowDialog();
            save.ShowDialog();

            string source = File.ReadAllText(open.FileName);

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

    public class vm
    {
        private int breakTime = 0;
        private String[] code;
        private String[,] codeClear;
        private int i, j = 0;
        // itérateur
        // code
        private String[] instructTraitement;

        // break time between two instruction
        private int ip = 0;

        private Boolean START = false;
        // ip
        // codeClear
        private int value1int, value2int;
        private String value1string, value2string;

        public void execute(String[,] codeClear, Form1 ui)
        {
            breakTime = Int32.Parse(ui.time.Text);
            while (ip != -1)
            {
                if (START == true)
                {
                    switch (codeClear[ip, 0])
                    {
                        case "NOP":         //nope
                            break;

                        case "PUSH":        //push value on the stack
                            ui.Invoke(ui.pushStack, codeClear[ip, 1]);
                            break;

                        case "POP":         //pop value on the stack
                            ui.Invoke(ui.popStack);
                            break;

                        case "ADD":         //integer addition
                            value1int = Int32.Parse(ui.listBox1.Items[0].ToString());
                            ui.Invoke(ui.popStack);
                            value2int = Int32.Parse(ui.listBox1.Items[0].ToString());
                            ui.Invoke(ui.popStack);
                            ui.Invoke(ui.pushStack, value1int + value2int);
                            break;

                        case "SUB":         //integer subtraction
                            value1int = Int32.Parse(ui.listBox1.Items[0].ToString());
                            ui.Invoke(ui.popStack);
                            value2int = Int32.Parse(ui.listBox1.Items[0].ToString());
                            ui.Invoke(ui.popStack);
                            ui.Invoke(ui.pushStack, value1int - value2int);
                            break;

                        case "MUL":         //integer multiplication
                            value1int = Int32.Parse(ui.listBox1.Items[0].ToString());
                            ui.Invoke(ui.popStack);
                            value2int = Int32.Parse(ui.listBox1.Items[0].ToString());
                            ui.Invoke(ui.popStack);
                            ui.Invoke(ui.pushStack, value1int * value2int);
                            break;

                        case "DIV":         //integer division
                            value1int = Int32.Parse(ui.listBox1.Items[0].ToString());
                            ui.Invoke(ui.popStack);
                            value2int = Int32.Parse(ui.listBox1.Items[0].ToString());
                            ui.Invoke(ui.popStack);
                            ui.Invoke(ui.pushStack, value1int / value2int);
                            break;

                        case "CMP":         //comparator
                            value1string = ui.listBox1.Items[0].ToString();
                            value2string = ui.listBox1.Items[1].ToString();
                            if (value1string.Equals(value2string))
                            {
                                ui.Invoke(ui.pushStack, "1");
                            }
                            else
                            {
                                ui.Invoke(ui.pushStack, "0");
                            }
                            break;

                        case "JMP":         //jump to instruction
                            ip = Int32.Parse(codeClear[ip, 1]) - 1;
                            break;

                        case "JE":          //jump if équal
                            value1string = ui.listBox1.Items[0].ToString();
                            ui.Invoke(ui.popStack);
                            if (value1string.Equals("1"))
                            {
                                ip = Int32.Parse(codeClear[ip, 1]) - 1;
                            }
                            break;

                        case "JNE":          //jump if not équal
                            value1string = ui.listBox1.Items[0].ToString();
                            ui.Invoke(ui.popStack);
                            if (value1string.Equals("0"))
                            {
                                ip = Int32.Parse(codeClear[ip, 1]) - 1;
                            }
                            break;

                        case "DUP":        //duplicate the element at the top of the stack
                            value1string = ui.listBox1.Items[0].ToString();
                            ui.Invoke(ui.pushStack, value1string);
                            break;

                        case "SWAP":       //exchange elements at the top of the stack
                            value1string = ui.listBox1.Items[0].ToString();
                            value2string = ui.listBox1.Items[1].ToString();
                            ui.Invoke(ui.popStack);
                            ui.Invoke(ui.popStack);
                            ui.Invoke(ui.pushStack, value1string);
                            ui.Invoke(ui.pushStack, value2string);
                            break;

                        case "PRINT":       //print
                            ui.Invoke(ui.printStack);
                            break;

                        case "READ":        //read
                            ui.Invoke(ui.pushStack, Microsoft.VisualBasic.Interaction.InputBox("READ", "READ"));
                            break;

                        case "END":         //exit the program
                            ip = -2;
                            break;

                        case null:          //no instruction ?
                            Console.WriteLine("pépin_1.");
                            break;

                        default:            //unknown instruction ?
                            Console.WriteLine("pépin_2.");
                            break;
                    }
                    ui.Invoke(ui.moveCursor, ip);
                    Thread.Sleep(breakTime);
                }
                else
                {
                    switch (codeClear[ip, 0])
                    {
                        case "START":         //start
                            START = true;
                            break;
                    }
                }
                ip++;
                ui.threadPauseResume.WaitOne(Timeout.Infinite);
            }
        }

        public void justDoIt(String textBoxContent, Form1 ui)
        {
            this.traitement(textBoxContent);
            this.execute(codeClear, ui);
        }

        public void traitement(String textBoxContent)
        {
            code = textBoxContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            codeClear = new string[code.Length, 5];
            foreach (String instruction in code)
            {
                instructTraitement = instruction.Split(new[] { " " }, StringSplitOptions.None);
                codeClear[i, 0] = instructTraitement[0];
                for (j = 1; j < instructTraitement.Length; j++)
                {
                    if (instructTraitement[j] != null)
                    {
                        if (j == 1)
                        {
                            codeClear[i, 1] += instructTraitement[j];
                        }
                        else
                        {
                            codeClear[i, 1] = codeClear[i, 1] + " " + instructTraitement[j];
                        }
                    }
                }
                i++;
            }
        }
    }
}