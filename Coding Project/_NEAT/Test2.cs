using System;
using System.Collections.Generic;
using System.Text;

namespace _NEAT
{
    public class Test2
    {
        Random r;
        List<(int i, int o, int innov)> global_connections;
        List<int> global_nodes;
        int inputs_amount;
        int outputs_amount;

        public Test2()
        {
            r = new Random();
            global_connections = new List<(int i, int o, int innov)>();
            global_nodes = new List<int>();
            inputs_amount = 11;
            outputs_amount = 2;

            global_connections.Add((0, 11, 0));
            global_connections.Add((0, 12, 1));
            global_connections.Add((1, 11, 2));
            global_connections.Add((1, 12, 3));
            global_connections.Add((2, 11, 4));
            global_connections.Add((2, 12, 5));
            global_connections.Add((3, 11, 6));
            global_connections.Add((3, 12, 7));
            global_connections.Add((4, 11, 8));
            global_connections.Add((4, 12, 9));
            global_connections.Add((5, 11, 10));
            global_connections.Add((5, 12, 11));
            global_connections.Add((6, 11, 12));
            global_connections.Add((6, 12, 13));
            global_connections.Add((7, 11, 14));
            global_connections.Add((7, 12, 15));
            global_connections.Add((8, 11, 16));
            global_connections.Add((8, 12, 17));
            global_connections.Add((9, 11, 18));
            global_connections.Add((9, 12, 19));
            global_connections.Add((10, 11, 20));
            global_connections.Add((10, 12, 21));
            global_connections.Add((0, 13, 22));
            global_connections.Add((13, 14, 23));
            global_connections.Add((14, 11, 24));
            global_connections.Add((14, 12, 25));
            global_connections.Add((3, 14, 26));
            global_connections.Add((7, 13, 27));
            for (int i = 0; i < 15; i++)
            {
                global_nodes.Add(i);
            }

            Brain b1 = new Brain(inputs_amount, outputs_amount);
            Brain b2 = new Brain(inputs_amount, outputs_amount);

            AddNode(10, 11, b2);
            AddNode(2, 11, b1);
            AddNode(3, 12, b1);
            b2.AddNode(10, 31, 33, 17);

            var test = Crossover(b1, b2).Guess(new List<double>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
        }

        private Brain Crossover(Brain b1, Brain b2)
        {
            // 0.75 => disabled gets inherited
            List<(Brain b, List<int> connections_innov)> parents = new List<(Brain, List<int>)>() { (b1, b1.GetInnovArray()), (b2, b2.GetInnovArray()) };
            int fitter = (b1.GetFittness() > b2.GetFittness()) ? 0 : 1;
            int longer = (b1.GetConnectionsCount() > b2.GetConnectionsCount()) ? 0 : 1;

            List<(int i, double w, int o, bool e, int innov)> offspring_connections = new List<(int i, double w, int o, bool e, int innov)>();

            foreach (var conn in global_connections)
            {
                if (parents[0].connections_innov.Contains(conn.innov) && parents[1].connections_innov.Contains(conn.innov))
                {
                    // matching connection
                    var connection = (r.NextDouble() < .5) ? parents[0].b.GetConnection(conn.innov) : parents[1].b.GetConnection(conn.innov);
                    if (connection.Item5 == conn.innov)
                        offspring_connections.Add(connection);
                }
                else if (!parents[fitter].connections_innov.Contains(conn.innov) && parents[Math.Abs(fitter - 1)].connections_innov.Contains(conn.innov))
                {
                    // unique connection in worse brain
                    var connection = parents[Math.Abs(fitter - 1)].b.GetConnection(conn.innov);
                    if (connection.Item5 == conn.innov)
                        offspring_connections.Add(connection);
                }
                else if (parents[fitter].connections_innov.Contains(conn.innov))
                {
                    // unique connection in fitter brain
                    var connection = parents[fitter].b.GetConnection(conn.innov);
                    if (connection.Item5 == conn.innov)
                        offspring_connections.Add(connection);
                }
            }

            Brain offspring = new Brain(inputs_amount, outputs_amount, offspring_connections);
            return offspring;
        }


        public void AddConnection(int from, int to, Brain b)
        { // TODO: check if connection already exists
            if (from >= global_nodes.Count || to >= global_nodes.Count)
            {
                throw new ArgumentException("Node not found!");
            }
            global_connections.Add((from, to, global_connections.Count));

            if (b != null)
            {
                b.AddConnection(from, to, global_connections.Count - 1);
            }
        }

        public void AddNode(int from, int to, Brain b)
        {
            if (from >= global_nodes.Count || to >= global_nodes.Count)
            {
                throw new ArgumentException("Node not found!");
            }
            global_nodes.Add(global_nodes.Count);
            AddConnection(from, global_nodes.Count - 1, null);
            AddConnection(global_nodes.Count - 1, to, null);
            //b.AddNode(from, to, global_connections.Count - 2, global_connections.Count - 1, global_nodes.Count - 1);
        }

    }
}
