//using System;
//using System.Collections.Generic;

//namespace _NEAT
//{
//    public class Node
//    {
//        readonly Random r;
//        readonly char type;
//        readonly List<Connection> inputs;
//        readonly List<Connection> outputs;
//        private double result;
//        private int gotten_inputs;
//        private bool resultFlag;
//        private int inx;
//        private int generation;

//        public char Type { get { return type; } }
//        public int Inx { get { return inx; } }
//        public int Generation { get { return generation; } }
//        public bool ResultFlag { get { return resultFlag; } }
//        public double Result { get { return result; } }

//        public Node(char type, int index, int generation)
//        {
//            r = new Random();
//            inx = index;
//            this.type = type;
//            this.generation = generation;
//            inputs = new List<Connection>();
//            outputs = new List<Connection>();
//            resultFlag = false;
//            gotten_inputs = 0;
//            result = 0;
//        }

//        #region - public functions -

//        public void InjectInput(double num)
//        {
//            result = num;
//            ActivateResult();
//            SendResult();
//        }

//        public void AddValue(int index, double val)
//        {
//            resultFlag = false;
//            result += val * GetWeightFromConnection(index);
//            gotten_inputs += 1;
//            if (gotten_inputs >= inputs.Count)
//            {
//                ActivateResult();
//                SendResult();
//            }
//        }

//        public bool AddIn(Connection c)
//        {
//            if (c.to != this) return false;
//            inputs.Add(c);
//            return true;
//        }

//        public bool RemoveIn(int index)
//        {
//            for (int i = 0; i < inputs.Count; i++)
//                if (inputs[i].Innov == index)
//                    inputs.RemoveAt(i);
//            return false;
//        }

//        public bool AddOut(Connection c)
//        {
//            if (c.from != this) return false;
//            outputs.Add(c);
//            return true;
//        }

//        public bool RemoveOut(int index)
//        {
//            for (int i = 0; i < outputs.Count; i++)
//                if (outputs[i].Innov == index)
//                    outputs.RemoveAt(i);
//            return false;
//        }

//        public bool Equals(Node n)
//        {
//            if (type != n.type) return false;
//            return true;
//        }

//        #endregion

//        #region - private functions -

//        private double GetWeightFromConnection(int index)
//        {
//            foreach (Connection c in inputs)
//            {
//                if (c.from.inx == index)
//                {
//                    return c.Weight;
//                }
//            }
//            throw new KeyNotFoundException("Couldn't find node with specified index!");
//        }

//        private bool ActivateResult()
//        {
//            result = Math.Tanh(result);
//            return true;
//        }

//        private bool SendResult()
//        {
//            if (type == 'o')
//            {
//                SetResultFlag();
//                return true;
//            }
//            foreach (Connection c in outputs)
//            {
//                c.to.AddValue(inx, result);
//            }
//            CleanNode();
//            return true;
//        }

//        private void SetResultFlag()
//        {
//            resultFlag = true;
//        }

//        private void RemoveResultFlag()
//        {
//            resultFlag = false;
//        }

//        private void CleanNode()
//        {
//            result = 0;
//            gotten_inputs = 0;
//            RemoveResultFlag();
//        }

//        #endregion

//        #region - print functions -

//        public void PrintInList()
//        {
//            foreach (Connection c in inputs)
//            {
//                Console.Write(c.ToString());
//            }
//            Console.WriteLine();
//        }

//        public void PrintOutList()
//        {
//            foreach (Connection c in outputs)
//            {
//                Console.Write(c.ToString());
//            }
//            Console.WriteLine();
//        }

//        #endregion
//    }
//}
