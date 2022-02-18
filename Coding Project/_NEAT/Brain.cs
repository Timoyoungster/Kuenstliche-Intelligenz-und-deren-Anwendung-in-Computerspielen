using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace _NEAT
{
    public class Brain
    {
        Random r;
        List<(int i, double w, int o, bool e, int innov)> connections;
        List<int> needed_input_amount;
        int inputs_amount;
        int outputs_amount;
        double score;
        public bool to_eliminate;

        double c1;
        double c2;
        double c3;
        double threshold;

        public double Score { get { return score; } set { score = value; } }

        public Brain(int in_amount, int out_amount)
        {
            SetupFields(in_amount, out_amount);
            for (int i = 0; i < inputs_amount; i++)
            {
                for (int o = inputs_amount; o < inputs_amount + outputs_amount; o++)
                {
                    connections.Add((i, r.NextDouble(), o, true, connections.Count));
                }
            }
            AddNeededActivations(inputs_amount + outputs_amount, 1);
            for (int o = 0; o < outputs_amount; o++)
            {
                needed_input_amount[inputs_amount + o] = inputs_amount;
            }
        }

        public Brain(int in_amount, int out_amount, List<(int i, double w, int o, bool e, int innov)> conns)
        {
            SetupFields(in_amount, out_amount);
            int nodes_amount = conns.Select(x => (x.i > x.o) ? x.i : x.o).Max();
            AddNeededActivations(nodes_amount + 1, 0);
            for (int i = 0; i < inputs_amount; i++)
            {
                needed_input_amount[i] = 1;
            }
            foreach (var connection in conns)
            {
                AddConnection(connection.i, connection.w, connection.o, connection.e, connection.innov);
            }
        }

        #region public functions

        public void AddConnection(int from, int to, int innov)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].innov == innov)
                {
                    connections[i] = (connections[i].i, connections[i].w, connections[i].o, true, connections[i].innov);
                    return;
                }
            }
            AddConnection(from, to, innov, true);
        }

        public void AddNode(int innov, int innovLeft, int innovRight, int inx)
        {
            if (!connections.Select(x => x.innov).Contains(innov))
            {
                throw new ArgumentException("This connection doesn't exist in this genome!");
            }
            (int i, _, int o, _, _) = GetConnection(innov);
            if (i >= 0 && o >= 0) DeactivateConnection(innov);
            AddNeededActivations(inx - needed_input_amount.Count + 1, 0);
            AddConnection(i, inx, innovLeft, false);
            AddConnection(inx, o, innovRight);
        }

        public int FindConnection(int innov)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].innov == innov) return i;
            }
            return -1;
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
                foreach (var (i, w, o, e, innov) in connections)
                {
                    int inx = FindConnection(innov);
                    if (gotten_inputs[i] >= needed_input_amount[i] && !sent[inx])
                    {
                        if (e) results[o] += Activate(results[i]) * w;
                        gotten_inputs[o] += 1;
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

        public bool EqualStructure(Brain b)
        {
            List<int> shorter_genome = (b.connections.Count < connections.Count) ? b.GetInnovArray() : GetInnovArray();
            List<int> longer_genome = (b.connections.Count >= connections.Count) ? b.GetInnovArray() : GetInnovArray();

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

            double d = c1 * excess + c2 * disjoint + c3 * Math.Abs(avg_weight_diff);

            return d < threshold;
        }

        public double GetFittness()
        {
            return score;
        }

        public bool MutateWeights()
        {
            double val = r.NextDouble() * 2 - 1;
            double change_mode;
            for (int i = 0; i < connections.Count; i++)
            {
                change_mode = r.NextDouble();
                if (change_mode < 0.75)
                    connections[i] = (connections[i].i, connections[i].w + val, connections[i].o, connections[i].e, connections[i].innov);
                else if (change_mode < 0.85)
                    connections[i] = (connections[i].i, connections[i].w + (r.NextDouble() * 2 - 1), connections[i].o, connections[i].e, connections[i].innov);
            }
            return true;
        }

        public List<int> GetInnovArray()
        {
            return connections.Select(x => x.innov).ToList();
        }

        public (int, double, int, bool, int) GetConnection(int innovation_number)
        {
            int inx = FindConnection(innovation_number);
            if (inx >= 0)
            {
                return (connections[inx].i, connections[inx].w, connections[inx].o, connections[inx].e, connections[inx].innov);
            }
            return (-1, -1, -1, false, -1);
        }

        public int GetConnectionsCount()
        {
            return connections.Count;
        }

        public int[] GetConnections()
        {
            return connections.Select(x => x.innov).ToArray();
        }

        public void SetupFields(int in_amount, int out_amount)
        {
            inputs_amount = in_amount;
            outputs_amount = out_amount;
            c1 = 1;
            c2 = 1;
            c3 = 0.4;
            threshold = 3;
            score = 0;
            to_eliminate = false;
            r = new Random();
            connections = new List<(int, double, int, bool, int)>();
            needed_input_amount = new List<int>();
        }

        /// <summary>
        /// Gets a random node index.
        /// </summary>
        /// <returns>random node index</returns>
        public int GetRandomNode()
        {
            int result = -1;
            while (result < 0)
            {
                result = r.Next(needed_input_amount.Count);
                if (needed_input_amount[result] == 0)
                    result = -1;
            }
            return result;
        }

        /// <summary>
        /// Gets a random unique connection.
        /// </summary>
        /// <returns>new random unique connection touple</returns>
        public (int i, int o) GetNewConnection()
        {
            List<int> neurons = new List<int>();
            foreach (var conn in connections)
            {
                if (!neurons.Contains(conn.i)) neurons.Add(conn.i);
                if (!neurons.Contains(conn.o)) neurons.Add(conn.o);
            }

            List<(int, int)> possible_connections = new List<(int, int)>();
            foreach (int n1 in neurons)
            {
                foreach (int n2 in neurons)
                {
                    possible_connections.Add((n1, n2));
                }
            }
            for (int i = possible_connections.Count - 1; i >= 0; i--)
            {
                if (possible_connections[i].Item1 < outputs_amount + inputs_amount && possible_connections[i].Item1 >= inputs_amount)
                    possible_connections.RemoveAt(i);
                else if (possible_connections[i].Item2 < inputs_amount)
                    possible_connections.RemoveAt(i);
                else if (possible_connections[i].Item1 == possible_connections[i].Item2)
                    possible_connections.RemoveAt(i);
                else if (possible_connections[i].Item1 < outputs_amount + inputs_amount && possible_connections[i].Item2 < outputs_amount + inputs_amount)
                    possible_connections.RemoveAt(i);
                else if (ConnectionExists(possible_connections[i].Item1, possible_connections[i].Item2))
                    possible_connections.RemoveAt(i);
                else if (CreatesCircle(possible_connections[i].Item1, possible_connections[i].Item2))
                    possible_connections.RemoveAt(i);
            }
            if (possible_connections.Count != 0)
                return possible_connections[r.Next(possible_connections.Count)];
            return (-1, -1);
        }

        /// <summary>
        /// Checks if connection already exists in global genome.
        /// </summary>
        /// <param name="i">node index of connection start</param>
        /// <param name="o">node index of connection end</param>
        /// <returns>true if connection was found, else false</returns>
        public bool ConnectionExists(int i, int o)
        {
            if (i < 0 || o < 0) return false;
            foreach (var conn in connections)
            {
                if (conn.i == i && conn.o == o) return true;
                if (conn.o == i && conn.i == o) return true;
            }
            return false;
        }

        public bool CreatesCircle(int i, int o)
        {
            bool all_finished = false;
            List<int> followers = new List<int>() { o };
            List<int> temp_followers = new List<int>();
            while (!all_finished)
            {
                all_finished = true;
                temp_followers.Clear();
                foreach (var f in followers)
                {
                    foreach (var c in connections)
                    {
                        if (c.i == f)
                        {
                            if (c.o == i)
                            {
                                Console.WriteLine("Circle found!");
                                return true;
                            }
                            if (c.o >= outputs_amount + inputs_amount)
                            {
                                all_finished = false;
                                temp_followers.Add(c.o);
                            }
                        }
                    }
                }
                followers.Clear();
                foreach (var item in temp_followers)
                {
                    followers.Add(item);
                }
            }
            return false;
        }

        #endregion

        #region private functions

        private void AddConnection(int from, int to, int innov, bool check)
        {
            if (from >= needed_input_amount.Count || to >= needed_input_amount.Count || (check && needed_input_amount[to] == 0) || CreatesCircle(from, to)) return;
            connections.Add((from, r.NextDouble(), to, true, innov));
            needed_input_amount[to] += 1;
        }

        private void AddConnection(int from, double weight, int to, bool enabled, int innov)
        {
            if (CreatesCircle(from, to)) return;
            connections.Add((from, weight, to, enabled, innov));
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
            //return 0.5 * (1 + Math.Tanh(val / 2));
            return 1 / (1 + Math.Pow(Math.E, -4.9 * val));
            //return (val > 0) ? 1 : 0;
        }

        private double GetWeightAt(int innov)
        {
            return connections.Select(x => x).Where(x => x.innov == innov).FirstOrDefault().w;
        }

        private void DeactivateConnection(int innov)
        {

            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].innov == innov)
                {
                    connections[i] = (connections[i].i, connections[i].w, connections[i].o, false, connections[i].innov);
                    return;
                }
            }
        }

        private void ActivateConnection(int innov, int o)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].innov == innov)
                {
                    connections[i] = (connections[i].i, connections[i].w, connections[i].o, true, connections[i].innov);
                    return;
                }
            }
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
