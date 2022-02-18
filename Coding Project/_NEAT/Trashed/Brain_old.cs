//using System;
//using System.Collections.Generic;

//namespace _NEAT
//{
//    public class Brain
//    {
//        Random r;
//        private List<Connection_old> connections;
//        readonly List<Node_old> input_nodes;
//        readonly List<Node_old> output_nodes;
//        int current_generation;
//        public int score;
//        int node_index;
//        int connection_index;

//        public Brain(int amount_inputs, int amount_outputs)
//        {
//            r = new Random();
//            connections = new List<Connection_old>();
//            input_nodes = new List<Node_old>();
//            output_nodes = new List<Node_old>();
//            current_generation = 0;
//            node_index = 0;
//            connection_index = 0;

//            for (int i = 1; i <= amount_inputs; i++)
//            {
//                input_nodes.Add(new Node_old('i', i, 0));
//            }
//            for (int o = amount_inputs + 1; o <= amount_inputs + amount_outputs; o++)
//            {
//                output_nodes.Add(new Node_old('o', o, 0));
//            }
//            for (int i = 0; i < amount_inputs; i++)
//            {
//                for (int o = 0; o < amount_outputs; o++)
//                {
//                    connections.Add(new Connection_old(input_nodes[i], output_nodes[o], connections.Count, 0, null));
//                }
//            }
//        }

//        #region - public functions -

//        public bool OverrideConnections(List<Connection_old> conns)
//        {
//            connections = conns;
//            return true;
//        }

//        public List<Connection_old> GetConnections()
//        {
//            return connections;
//        }

//        public bool ConnectionExists(Connection_old c) // TODO: make sure that no duplicated indecies get created at merging brains 
//        {
//            if (c == null) return false;
//            foreach (Connection_old conn in connections)
//            {
//                if (conn.Equals(c)) return true;
//            }
//            return false;
//        } 

//        public Connection_old FindConnection(Connection_old c) // TODO: make sure that no duplicated indecies get created at merging brains 
//        {
//            if (c == null) return null;
//            foreach (Connection_old conn in connections)
//            {
//                if (conn.Equals(c)) return conn;
//            }
//            return null;
//        } 

//        public List<double> Guess(List<double> inputs)
//        {
//            List<double> result = new List<double>();
//            foreach (Node_old n in input_nodes)
//            {
//                n.InjectInput(inputs[0]);
//                inputs.RemoveAt(0);
//            }
//            foreach (Node_old n in output_nodes)
//            {
//                if (n.ResultFlag == true)
//                {
//                    result.Add(n.Result);
//                }
//                else
//                {
//                    throw new MissingFieldException("Output not finished!");
//                }
//            }
//            return result;
//        }

//        public bool CompareStructure(Brain b) // TODO: read speciation article and write function based on that 
//        {
//            bool amount_connections = connections.Count == b.connections.Count;
//            bool amount_connections_from_ins = true;
//            for (int i = 0; i < input_nodes.Count; i++)
//            {
//                //if (input_nodes[i].GetOutNodes().Count != b.input_nodes[i].GetOutNodes().Count)
//                //{
//                //    amount_connections_from_ins = false;
//                //    break;
//                //}
//            }
//            return amount_connections & amount_connections_from_ins;
//        }

//        public bool IncrementGen()
//        {
//            current_generation += 1;
//            return true;
//        }

//        public bool SetScore(int s)
//        {
//            score = s;
//            return true;
//        }

//        public bool AddNode(int left_inx, int middle_inx, int right_inx, int first_innov, int second_innov, int gen, double w)
//        {
//            //TODO: implement error checks
//            Node_old n = new Node_old('h', middle_inx, gen);
//            AddConnection(new Connection_old(FindNode(left_inx), n, first_innov, gen, 1));
//            AddConnection(new Connection_old(n, FindNode(right_inx), second_innov, gen, w));
//            return true;
//        }

//        public bool AddConnection(Connection_old c)
//        {
//            if (c == null) return false;

//            c.from.AddOut(c);
//            c.to.AddIn(c);
//            connections.Add(c);

//            return true;
//        }

//        public Node_old FindNode(int inx)
//        {
//            foreach (Connection_old c in connections)
//            {
//                if (c.from.Inx == inx) return c.from;
//                if (c.to.Inx == inx) return c.to;
//            }
//            throw new ArgumentException("Node with index could not be found!");
//        }

//        #endregion

//        #region - private functions -

//        //private int GetNodeIndex()
//        //{
//        //    node_index += 1;
//        //    return node_index;
//        //}

//        //private int GetConnectionIndex()
//        //{
//        //    connection_index += 1;
//        //    return connection_index;
//        //}

//        //private bool MutateWeight()
//        //{
//        //    GetRandomConnection().MutateWeight();
//        //    return true;
//        //}

//        //private bool MutateNode()
//        //{
//        //    Connection c = connections[r.Next(connections.Count)];  // selecting connection to reroute
//        //    if (c == null) return false;

//        //    // Removing connection references
//        //    c.Enabled = false;
//        //    c.from.RemoveOut(c.Innov);
//        //    c.to.RemoveIn(c.Innov);

//        //    // creating new node and connections
//        //    Node n = new Node('h', GetNodeIndex(), current_generation);
//        //    Connection leg1 = new Connection(c.from, n, c.Innov, current_generation, null); // TODO: keep an eye on c.Inx if it creates problems with previous connection ... 
//        //    Connection leg2 = new Connection(n, c.to, GetConnectionIndex(), current_generation, null);

//        //    // create input/output list entries
//        //    c.from.AddOut(leg1);
//        //    n.AddIn(leg1);
//        //    n.AddOut(leg2);
//        //    c.to.AddIn(leg2);
//        //    connections.Add(leg1);
//        //    connections.Add(leg2);

//        //    return true;
//        //}

//        //private bool MutateConnection()
//        //{
//        //    Connection c = CreateRandomUniqueConnection();
//        //    if (c == null) return false;
//        //    c.from.AddOut(c);
//        //    c.to.AddIn(c);
//        //    connections.Add(c);
//        //    return true;
//        //}

//        //private Node GetRandomNode()
//        //{
//        //    int inx = r.Next(1, node_index + 1);
//        //    foreach (Connection c in connections)
//        //    {
//        //        if (c.from.Inx == inx) return c.from;

//        //        if (c.to.Inx == inx) return c.to;
//        //    }
//        //    return null;
//        //}

//        //private Connection GetRandomConnection()
//        //{
//        //    int index = r.Next(connections.Count);
//        //    return connections[index];
//        //}

//        //private Connection CreateRandomUniqueConnection()
//        //{
//        //    Node n1;
//        //    Node n2;

//        //    int temp_index = GetConnectionIndex();
//        //    Connection c;

//        //    for (int i = 0; i < node_index; i++)
//        //    {
//        //        n1 = GetRandomNode();
//        //        n2 = GetRandomNode();

//        //        if (n1 == n2)
//        //            continue;
//        //        if (n1.Type == 'i' && n2.Type == 'i')
//        //            continue;
//        //        if (n1.Type == 'o' && n2.Type == 'o')
//        //            continue;

//        //        // EVENTUALLY: check for usefulness of connection

//        //        c = new Connection(n1, n2, temp_index, current_generation, null);
//        //        Connection found = FindConnection(c);
//        //        if (found == null)
//        //        {
//        //            return c;
//        //        }
//        //        else if (found.Enabled == false)
//        //        {
//        //            found.Enabled = true;
//        //            found.from.AddOut(found);
//        //            found.to.AddIn(found);
//        //            Console.WriteLine("Connection enabled.");
//        //            return null; // returns null => no mutation happened would get printed
//        //        }
                
//        //    }
//        //    return null;
//        //}



//        #endregion
//    }
//}
