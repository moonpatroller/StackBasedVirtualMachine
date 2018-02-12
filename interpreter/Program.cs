#define DEBUG

using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;


namespace interpreter
{

    public class vm
    {

        private int i, j, k, ip, value1int, value2int;
        private string value1string, value2string;
        private string codeTrait;
        private string[] codeSplit, instructTraitement;
        private string[] instruction = { "START", "NOP", "PUSH", "POP", "ADD", "SUB", "MUL", "DIV", "CMP", "JMP", "JE", "JNE", "DUP", "SWAP", "PRINT", "READ", "POS", "INC", "DEC", "END" };
        private string[] opCode = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F", "10", "11", "12", "13", "FF" };
        private string[,] codeClear;
        private bool START = false;
        private Stack stack = new Stack();

        public void ADD()
        {
            //integer addition
            value1int = int.Parse(stack.Pop().ToString());
            value2int = int.Parse(stack.Pop().ToString());
            stack.Push(value1int + value2int);
        }

        public void INC()
        {
            //incrémentation
            value1int = int.Parse(stack.Pop().ToString());
            stack.Push(value1int + 1);
        }

        public void DEC()
        {
            //décrémentation
            value1int = int.Parse(stack.Pop().ToString());
            stack.Push(value1int - 1);
        }

        public void CMP()
        {
            //comparator
            value1string = stack.Pop().ToString();
            value2string = stack.Peek().ToString();
            stack.Push(value1string);
            if (value1string.Equals(value2string))
            {
                stack.Push(1);
            }
            else
            {
                stack.Push(0);
            }
        }

        public void DIV()
        {
            //integer division
            value1int = int.Parse(stack.Pop().ToString());
            value2int = int.Parse(stack.Pop().ToString());
            stack.Push(value1int / value2int);
        }

        public void DUP()
        {
            //duplicate the element at the top of the stack
            stack.Push(stack.Peek());
        }

        public void END()
        {
            //exit the program
            ip = -2;
        }

        public void JE(string jumpTo)
        {
            //jump if équal
            value1string = stack.Pop().ToString();
            if (value1string.Equals("1"))
            {
                ip = int.Parse(jumpTo) - 1;
            }
        }

        public void JMP(string jumpTo)
        {
            //jump to instruction
            ip = int.Parse(jumpTo) - 1;
        }

        public void JNE(string jumpTo)
        {
            //jump if not équal
            value1string = stack.Pop().ToString();
            if (value1string.Equals("0"))
            {
                ip = int.Parse(jumpTo) - 1;
            }
        }

        public void MUL()
        {
            //integer multiplication
            value1int = int.Parse(stack.Pop().ToString());
            value2int = int.Parse(stack.Pop().ToString());
            stack.Push(value1int * value2int);
        }

        public void NOP()
        {
            //NOP
        }

        public void POP()
        {
            //pop
            stack.Pop();
        }

        public void POS()
        {
            //set cursor pos
            value1int = int.Parse(stack.Pop().ToString());
            value2int = int.Parse(stack.Pop().ToString());
            Console.SetCursorPosition(value1int, value2int);
        }

        public void PRINT()
        {
            //print
            Console.WriteLine(stack.Peek());
        }

        public void PUSH(string pToStack)
        {
            //push value on the stack
            stack.Push(pToStack);
        }

        public void READ()
        {
            //read
            stack.Push(Console.ReadLine());
        }

        public void SUB()
        {
            //integer subtraction
            value1int = int.Parse(stack.Pop().ToString());
            value2int = int.Parse(stack.Pop().ToString());
            stack.Push(value1int - value2int);
        }

        public void SWAP()
        {
            //exchange elements at the top of the stack
            value1string = stack.Pop().ToString();
            value2string = stack.Pop().ToString();
            stack.Push(value2string);
            stack.Push(value1string);
        }

        public void traitement(string codeHex)
        {
            for (i = 0; i < codeHex.Length; i = i + 2)
            {
                codeTrait = codeTrait + codeHex.ElementAt(i) + codeHex.ElementAt(i + 1) + ".";
            }
            codeTrait = codeTrait.Replace("00.5E", " -");
            codeTrait = codeTrait.Replace("3F", " ");
            codeClear = new string[codeTrait.Length, 3];
            for (i = 0; i < opCode.Length; i++)
            {
                codeTrait = codeTrait.Replace(opCode[i] + ".", instruction[i]);
            }
            codeSplit = Regex.Split(codeTrait, "-");
            for (i = 0; i < codeSplit.Length; i++)
            {
                instructTraitement = codeSplit[i].Split(new char[] { ' ' }, 2);
                codeClear[i, 0] = instructTraitement[0].Replace(".", "");
                if (instructTraitement.Length > 1)
                {
                    if (instructTraitement[1] != "")
                    {
                        codeClear[i, 1] = codeSplit[i].Replace(instructTraitement[0], "").Replace(".", "").Replace(" ", "20");

                        byte[] raw = new byte[codeClear[i, 1].Length / 2];
                        for (j = 0; j < raw.Length; j++)
                        {
                            raw[j] = Convert.ToByte(codeClear[i, 1].Substring(j * 2, 2), 16);
                        }
                        codeClear[i, 1] = Encoding.ASCII.GetString(raw).Substring(1);
                    }
                }
            }
        }


        public void execute(string[,] codeClear)
        {
            ip = 0;
#if DEBUG
            Stopwatch sw = new Stopwatch();
#endif
            while (ip != -1)
            {
                if (START)
                {
#if DEBUG
                    k = 0;
                    if (sw.Elapsed.TotalMilliseconds > 0)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(sw.Elapsed.TotalMilliseconds.ToString() + " MS");
                    }

                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.Write(ip.ToString() + ".");
                    while (codeClear[ip, k] != null)
                    {
                        Console.Write(codeClear[ip, k] + " ");
                        k++;
                    }
                    Console.WriteLine();
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    sw.Start();
#endif
                    MethodInfo mi = GetType().GetMethod(codeClear[ip, 0]);
#if DEBUG
                    sw.Start();
#endif
                    try
                    {
                        if (codeClear[ip, 1] != null)
                        {
                            mi.Invoke(this, new object[] { codeClear[ip, 1] });
                        }
                        else
                        {
                            mi.Invoke(this, null);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("error");
                    }
#if DEBUG
                    sw.Stop();
#endif
                }
                else
                {
                    switch (codeClear[ip, 0])
                    {
                        case "START":  //start
                            START = true;
                            break;
                    }
                }
                ip++;
            }
        }


        public void justDoIt(string codeHex)
        {
            traitement(codeHex);
            execute(codeClear);
            Console.ReadLine();
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            string program;
            try
            {
                ResourceManager RM = new ResourceManager("interpreter.Properties.Resources", Assembly.GetExecutingAssembly());
                program = RM.GetString("ProgramBin");
            }
            catch
            {
                program = "01005E033F48454C4C4F3F574F524C44005E0F005EFF";
            }
            vm virtualMachine = new vm();
            virtualMachine.justDoIt(program);
        }
    }
}