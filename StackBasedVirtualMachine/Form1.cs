using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public delegate void printStackHandler();
        public delegate void pushListBoxHandler(object element);
        public delegate void popListBoxHandler();
        public delegate void moveCursorHandler(int Position);
        public printStackHandler  printStack;
        public pushListBoxHandler pushStack;
        public popListBoxHandler popStack;
        public moveCursorHandler moveCursor;
        public ManualResetEvent threadPauseResume = new ManualResetEvent(true);
        
        Thread vmThread;

        public Form1()
        {
            printStack = new printStackHandler(printTextarea);
            pushStack = new pushListBoxHandler(pushListBox);
            popStack = new popListBoxHandler(popListBox);
            moveCursor = new moveCursorHandler(moveCursorTo);
            InitializeComponent();
        }

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


        private void pushListBox(object element)
        {
            listBox1.Items.Insert(0, element);
        }

        private void popListBox()
        {
            listBox1.Items.RemoveAt(0);
        }

        private void printTextarea()
        {
            textBox2.AppendText(listBox1.Items[0].ToString() + "\r\n");
        }
        private void moveCursorTo(int Position)
        {
            pictureBox1.Location = new Point(10, (Position * 15)+3); ;
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

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItems.Count == 1)
            {
                String currentStack = listBox1.SelectedItem.ToString();
                listBox1.Items[listBox1.SelectedIndex] = Microsoft.VisualBasic.Interaction.InputBox("READ", "READ", currentStack);
            }
            
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

    }

    public class vm
    {
        private Boolean START = false;
        private int breakTime = 0;                  // break time between two instruction
        private int ip = 0;                         // ip
        private int i, j = 0;                       // itérateur
        private String[] code;                      // code
        private String[] instructTraitement;
        private String[,] codeClear;                // codeClear
        private int value1int, value2int;
        private String value1string, value2string;
        
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
                            codeClear[i, 1]+= instructTraitement[j];
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
    }
}