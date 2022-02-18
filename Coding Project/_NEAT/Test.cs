using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class Test
    {
        Random r;
        List<(int i, double w, int o, bool e, int innov)> connections;
        List<int> needed_input_amount;
        int inputs_amount;
        int outputs_amount;
        int score;

        double c1;
        double c2;
        double c3;
        double threshold;

        public int Score { get { return score; } set { score = value; } }

        public Test(int in_amount, int out_amount)
        {
            inputs_amount = in_amount;
            outputs_amount = out_amount;
            c1 = 1;
            c2 = 1;
            c3 = 0.4;
            threshold = 3;
            score = 0;
            r = new Random();
            connections = new List<(int, double, int, bool, int)>();
            needed_input_amount = new List<int>();
            for (int i = 0; i < inputs_amount; i++)
            {
                for (int o = inputs_amount; o < inputs_amount + outputs_amount; o++)
                {
                    connections.Add((i, 0.5, o, true, connections.Count));
                }
            }
            AddNeededActivations(inputs_amount + outputs_amount, 1);
            for (int o = 0; o < outputs_amount; o++)
            {
                needed_input_amount[inputs_amount + o] = inputs_amount;
            }

            AddNode(0, 4, connections.Count, connections.Count + 1, 5);
            AddConnection(1, 5, connections.Count);
            AddConnection(5, 3, connections.Count);
            AddNode(0, 5, connections.Count, connections.Count + 1, 7);
            AddConnection(7, 3, 30);
        }

        #region public functions

        public void AddConnection(int from, int to, int innov)
        {
            AddConnection(from, to, innov, true);
        }

        public void AddNode(int from, int to, int innovLeft, int innovRight, int inx)
        {
            if (from >= needed_input_amount.Count || to >= needed_input_amount.Count)
                throw new ArgumentException("Node not found!");

            AddNeededActivations(inx - needed_input_amount.Count + 1, 0);
            AddConnection(from, inx, innovLeft, false);
            AddConnection(inx, to, innovRight);
        }

        public void ToggleEnableBit(int innov)
        {
            int inx = FindConnection(innov);
            needed_input_amount[connections[inx].o] -= 1;
            connections[inx] = (connections[inx].i, connections[inx].w, connections[inx].o, !connections[inx].e, connections[inx].innov);
        }

        public int FindConnection(int innov)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].innov == innov) return i;
            }
            throw new ArgumentException("Innovation Number not found!");
        }

        public List<double> Guess(List<double> inputs)
        {
            double[] results = new double[needed_input_amount.Count];
            int[] gotten_inputs = new int[needed_input_amount.Count];
            bool[] sent = new bool[connections.Count];

            for (int i = 0; i < inputs_amount; i++)
            {
                results[i] = inputs[i];
                gotten_inputs[i] += 1;
            }

            do
            {
                foreach (var c in connections)
                {
                    int inx = FindConnection(c.innov);
                    if (gotten_inputs[c.i] >= needed_input_amount[c.i] && !sent[inx] && c.e)
                    {
                        results[c.o] += Activate(results[c.i]) * c.w;
                        gotten_inputs[c.o] += 1;
                        sent[inx] = true;
                    }
                    else if (!c.e && !sent[inx])
                    {
                        sent[inx] = true;
                    }
                }
            } while (sent.Select(x => x).Where(x => x == false).ToArray().Length > 0);

            List<double> outputs = new List<double>();
            for (int i = inputs_amount; i < inputs_amount + outputs_amount; i++)
            {
                outputs.Add(Activate(results[i]));
            }
            return outputs;
        }

        public bool EqualStructure(Test b)
        {
            List<int> shorter_genome = (b.connections.Count < connections.Count) ? b.GetInnovArray() : GetInnovArray();
            List<int> longer_genome = (b.connections.Count >= connections.Count) ? b.GetInnovArray() : GetInnovArray();

            double N = longer_genome.Count;

            shorter_genome.Sort();
            longer_genome.Sort();

            double disjoint = 0;
            double excess = 0;
            double matching = 0;
            double avg_weight_diff = 0;

            for (int i = 0; i < shorter_genome.Count;)
            {
                if (longer_genome.IndexOf(shorter_genome[i]) >= 0)
                {
                    matching += 1;
                    avg_weight_diff += GetWeightAt(shorter_genome[i]) - b.GetWeightAt(shorter_genome[i]);
                    longer_genome.Remove(shorter_genome[i]);
                    shorter_genome.Remove(shorter_genome[i]);
                }
                else
                {
                    i += 1;
                }
            }

            avg_weight_diff = avg_weight_diff / matching;

            disjoint += shorter_genome.Count;

            int last_disjoint_innov = (shorter_genome.Count > 0) ? shorter_genome.Last() : -1;

            foreach (int innov in longer_genome)
            {
                if (innov > last_disjoint_innov) excess += 1;
                else disjoint += 1;
            }

            double d = c1 * excess / N + c2 * disjoint / N + c3 * avg_weight_diff;

            return d < threshold;
        }

        public bool MutateWeights()
        {
            // 0.9 => gleichmäßig
            // 0.1 => random
            throw new NotImplementedException();
        }

        #endregion

        #region private functions

        private void AddConnection(int from, int to, int innov, bool check)
        {
            if (from >= needed_input_amount.Count || to >= needed_input_amount.Count || (check && needed_input_amount[to] == 0))
            {
                throw new ArgumentException("Node not found!");
            }
            connections.Add((from, 0.5, to, true, innov));
            needed_input_amount[to] += 1;
        }

        private void AddNeededActivations(int amount, int init_value)
        {
            if (amount <= 0) return;
            for (int i = 0; i < amount; i++)
            {
                needed_input_amount.Add(init_value);
            }
        }

        private double Activate(double val)
        {
            return 0.5 * (1 + Math.Tanh(val / 2));
        }

        public int GetConnectionsCount()
        {
            return connections.Count;
        }

        private List<int> GetInnovArray()
        {
            return connections.Select(x => x.innov).ToList();
        }

        private double GetWeightAt(int innov)
        {
            return connections.Select(x => x).Where(x => x.innov == innov).FirstOrDefault().w;
        }

        #endregion

        public override string ToString()
        {
            string output = "";

            foreach (var c in connections)
            {
                output += c;
            }

            return output;
        }
    }
}
