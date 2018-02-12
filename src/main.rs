use std::io::Read;
use std::collections::HashMap;

macro_rules! map(
    { $($key:expr => $value:expr),+ } => {
        {
            let mut m = ::std::collections::HashMap::new();
            $(
                m.insert($key, $value);
            )+
            m
        }
     };
);

fn main() {
    PileVirt::new(true).execute(PileVirt::compile("START
NOP
PUSH 5
DEC
DUP
PUSH 0
CMP
PRINT_STACK
JNE 3
PUSH 9
PRINT_STACK
SWAP
PRINT_STACK
END", true));
}

#[derive(Debug)]
pub struct PileVirt {
    is_debug: bool,
    stack: Vec<i32>,
    ip: i32
}

impl PileVirt {

    pub fn new(is_debug: bool) -> PileVirt {
        PileVirt {
            is_debug: is_debug,
            stack: Vec::new(),
            ip: 0
        }
    }

    pub fn inc(&mut self) {
        *self.stack.last_mut().unwrap() += 1;
    }

    pub fn dec(&mut self) {
        *self.stack.last_mut().unwrap() -= 1;
    }

    pub fn add(&mut self) {
        let second = self.stack.pop().expect("Stack value missing for add.");
        self.stack.last_mut().map(|x| *x + second);
    }

    pub fn sub(&mut self) {
        let second = self.stack.pop().expect("Stack value missing for sub.");
        self.stack.last_mut().map(|x| *x - second);
    }

    pub fn mul(&mut self) {
        let second = self.stack.pop().expect("Stack value missing for mul.");
        self.stack.last_mut().map(|x| *x * second);
    }

    pub fn div(&mut self) {
        let second = self.stack.pop().expect("Stack value missing for div.");
        self.stack.last_mut().map(|x| *x / second);
    }

    pub fn pop(&mut self) {
        self.stack.pop();
    }

    pub fn push(&mut self, value: i32) {
        self.stack.push(value);
    }

    pub fn dup(&mut self) {
        let last = self.stack.len() - 1;
        let last_el = self.stack[last];
        self.stack.push(last_el);
    }

    pub fn swap(&mut self) {
        let last_index = self.stack.len() - 1;
        let second_last_index = self.stack.len() - 2;
        let last = self.stack[last_index];
        let second_last = self.stack[second_last_index];
        *self.stack.last_mut().unwrap() = second_last;
        *self.stack.get_mut(second_last_index).unwrap() = last;
    }

    pub fn cmp(&mut self) {
        let second = self.stack.pop();
        let first = self.stack.pop();
        self.stack.push(if first == second { 1 } else { 0 });
    }

    pub fn je(&mut self, addr: i32) {
        let top = self.stack.pop().expect("Stack value missing for je.");
        if top == 1 {
            self.ip = addr;
        }
    }

    pub fn jne(&mut self, addr: i32) {
        let top = self.stack.pop().expect("Stack value missing for jne.");
        if top == 0 {
            self.ip = addr;
        }
    }

    pub fn jmp(&mut self, addr: i32) {
        self.ip = addr;
    }

    pub fn end(&mut self) {
        self.ip = -2;
    }

    pub fn nop(&self) {
    }

    pub fn print(&mut self) {
        let top = self.stack.pop().expect("Stack value missing for print.");
        println!("{}", top);
    }

    pub fn print_stack(&self) {
        println!("{:?}", self.stack);
    }

    pub fn read(&mut self) {
        let input: i32 = std::io::stdin()
            .bytes() 
            .next()
            .and_then(|result| result.ok())
            .map(|byte| byte as i32).expect("Couldn't read byte from stdin.");

        self.stack.push(input);
    }

    pub fn over(&mut self) {
        let second_last_index = self.stack.len() - 2;
        let second_last = self.stack[second_last_index];
        self.stack.push(second_last);
    }

    pub fn compile(input: &str, is_debug: bool) -> Vec<i32> {
        if is_debug {
            println!("Compiling token: {:?}", input.split_whitespace().collect::<Vec<_>>());
        }
        let tokens = input.split_whitespace();
        let instrs: HashMap<&str, i32> = map!{
            "START" => 1,
            "NOP"   => 2,
            "PUSH"  => 3, 
            "POP"  => 4, 
            "ADD"  => 5, 
            "SUB"  => 6, 
            "MUL"  => 7, 
            "DIV"  => 8, 
            "CMP"  => 9, 
            "JMP"  => 10, 
            "JE"   => 11, 
            "JNE"  => 12, 
            "DUP"  => 13, 
            "SWAP" => 14, 
            "PRINT" => 15, 
            "READ" => 16, 
            "POS"  => 17, 
            "INC"  => 18, 
            "DEC"  => 19, 
            "PRINT_STACK" => 20, 
            "END"  => 255
        };
        let op_codes = tokens.map(|t| {
            instrs.get(t).map(|r| *r).unwrap_or_else(|| t.parse::<i32>().unwrap())
        });
        let op_code_vec = op_codes.collect::<Vec<i32>>();
        if is_debug {
            println!("Compiled op codes: {:?}", op_code_vec);
        }
        op_code_vec
    }

    pub fn execute(&mut self, instrs: Vec<i32>) {
        self.ip = 0;
        while self.ip >= 0 {
            let ip = self.ip as usize;
            match instrs[ip] {
                1 => {
                    // self.start();
                    self.ip += 1;
                }
                2 => {
                    self.nop();
                    self.ip += 1;
                }
                3 => {
                    self.ip += 1;
                    self.push(instrs[ip + 1]);
                    self.ip += 1;
                }
                4 => {
                    self.pop();
                    self.ip += 1;
                }
                5 => {
                    self.add();
                    self.ip += 1;
                }
                6 => {
                    self.sub();
                    self.ip += 1;
                }
                7 => {
                    self.mul();
                    self.ip += 1;
                }
                8 => {
                    self.div();
                    self.ip += 1;
                }
                9 => {
                    self.cmp();
                    self.ip += 1;
                }
                10 => {
                    self.ip += 1;
                    self.jmp(instrs[ip + 1]);
                    self.ip += 1;
                }
                11 => {
                    self.ip += 1;
                    self.je(instrs[ip + 1]);
                    self.ip += 1;
                }
                12 => {
                    self.ip += 1;
                    self.jne(instrs[ip + 1]);
                    self.ip += 1;
                }
                13 => {
                    self.dup();
                    self.ip += 1;
                }
                14 => {
                    self.swap();
                    self.ip += 1;
                }
                15 => {
                    self.print();
                    self.ip += 1;
                }
                16 => {
                    self.read();
                    self.ip += 1;
                }
                17 => {
                    // self.pos();
                    self.ip += 1;
                }
                18 => {
                    self.inc();
                    self.ip += 1;
                }
                19 => {
                    self.dec();
                    self.ip += 1;
                }
                20 => {
                    self.print_stack();
                    self.ip += 1;
                }
                _ => {
                    self.end();
                }
            }
        }
    }
}
