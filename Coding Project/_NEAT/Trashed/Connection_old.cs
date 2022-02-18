//using System;

//namespace _NEAT
//{
//    public class Connection : IComparable
//    {
//        Random r;
//        public readonly Node from;
//        public readonly Node to;
//        private double weight;
//        private bool enabled;
//        private int innov;
//        readonly int generation;

//        public double Weight { get { return weight; } set { weight = value; } }
//        public bool Enabled { get { return enabled; } set { enabled = value; } }
//        public int Innov { get { return innov; } }

//        public Connection(Node from, Node to, int innov, int generation, double? weight)
//        {
//            r = new Random();
//            this.from = from;
//            this.to = to;
//            this.innov = innov;
//            enabled = true;
//            this.generation = generation;
//            if (weight == null)
//                this.weight = r.NextDouble();
//            else
//                this.weight = (double)weight;
//        }

//        public bool MutateWeight()
//        {
//            weight = r.NextDouble();
//            return true;
//        }

//        public int CompareTo(object obj)
//        {
//            return innov.CompareTo((obj as Connection).Innov);
//        }

//        public bool Equals(Connection c)
//        {
//            if (from.Inx == c.from.Inx && to.Inx == c.to.Inx) return true;
//            if (from.Inx == c.to.Inx && to.Inx == c.from.Inx) return true;
//            return false;
//        }

//        override public string ToString()
//        {
//            return from.Inx + " --> " + to.Inx;
//        }
//    }
//}
