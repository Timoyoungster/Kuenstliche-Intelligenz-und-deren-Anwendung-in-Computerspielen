using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Transactions;

namespace _NEAT
{
    public class Game1 : Game
    {

        #region fields

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        List<Texture2D> textures;
        SpriteFont font;

        readonly Random r = new Random();

        int width;
        int height;

        // Game fields
        Player[] players;
        List<Meteorite> debris; // diameter 40 - 80
        List<double> outputs;
        double current_score; // gets incremented on meteorite deletion
        int highscore;
        int debris_amount;
        double scanbeam_length;
        int gold_meteorite_points;
        bool unlock_time_score;

        // NEAT fields
        int brains_amount;
        int inputs_amount;
        int outputs_amount;
        List<Brain> brains;
        int generation;
        List<(int i, int o, int innov)> global_connections;
        List<int> global_nodes;
        double mutation;
        double weight_mutation;
        double node_mutation;
        double connection_mutation;
        double species_crossover;
        double inherit_disabled;

        #endregion

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        #region monogame functions

        protected override void Initialize()
        {

            width = GraphicsDevice.Viewport.Width;
            height = GraphicsDevice.Viewport.Height;

            // Game settings
            debris_amount = 20;
            scanbeam_length = Math.Pow(300, 2);

            debris = new List<Meteorite>();
            outputs = new List<double>();
            current_score = 0;
            highscore = 0;
            gold_meteorite_points = 1;
            unlock_time_score = false;


            // Network settings
            brains_amount = 150;
            inputs_amount = 9;
            outputs_amount = 2;
            mutation = .25;
            weight_mutation = .85;
            node_mutation = .005;
            connection_mutation = .01;
            species_crossover = .001;
            inherit_disabled = .75;

            generation = 0;
            brains = new List<Brain>();
            global_connections = new List<(int i, int o, int innov)>();
            global_nodes = new List<int>();

            SetupInitialGenome();

            for (int i = 0; i < brains_amount; i++)
            {
                brains.Add(new Brain(inputs_amount, outputs_amount));
            }

            textures = new List<Texture2D>();


            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            font = Content.Load<SpriteFont>("Font");
            textures.Add(Content.Load<Texture2D>("Spaceship"));
            textures.Add(Content.Load<Texture2D>("Meteorite"));
            textures.Add(Content.Load<Texture2D>("Gold"));
            players = new Player[brains_amount];
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = new Player(width / 2, height / 2, textures[0].Width, width, height, brains[i]);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var kstate = Keyboard.GetState();

            if (kstate.IsKeyDown(Keys.F))
            {
                for (int i = 0; i < 10; i++)
                {
                    UpdateSpaceGame();
                }
            }

            if (kstate.IsKeyDown(Keys.F) && kstate.IsKeyDown(Keys.LeftControl))
            {
                int target_gen = generation + 10;
                while (generation < target_gen)
                {
                    UpdateSpaceGame();
                }
            }
            if (kstate.IsKeyDown(Keys.G) && kstate.IsKeyDown(Keys.LeftControl))
            {
                int target_gen = generation + 100;
                while (generation < target_gen)
                {
                    UpdateSpaceGame();
                }
            }

            UpdateSpaceGame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            foreach (Player p in players)
            {
                if (!p.dead) _spriteBatch.Draw(textures[0], new Vector2((float)p.GetX(), (float)p.GetY()), null, Color.White, (float)(Math.PI / 180 * (p.GetR() + 90)), new Vector2(textures[0].Width / 2, textures[0].Width / 2), 1, SpriteEffects.None, 0);
            }
            foreach (Meteorite m in debris)
            {
                _spriteBatch.Draw(textures[m.GetT() < 0 ? 1 : 2], new Vector2((float)m.GetX(), (float)m.GetY()), null, Color.White, 0, new Vector2(textures[1].Width / 2, textures[1].Height / 2), (float)m.GetD() / textures[0].Width, SpriteEffects.None, 0);
            }
            _spriteBatch.DrawString(font, "Score: " + current_score, new Vector2(20, 20), Color.White);
            _spriteBatch.DrawString(font, "Generation: " + generation, new Vector2(20, 50), Color.White);
            _spriteBatch.DrawString(font, "Highscore: " + highscore, new Vector2(20, 80), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        #endregion

        #region Game functions

        #region - private functions -

        /// <summary>
        /// Calculates output of current brain and updates the game accordingly.
        /// </summary>
        private void UpdateSpaceGame()
        {
            if (AllDead())
            {
                NextGeneration();
                ResetGame();
            }

            if (debris.Count < debris_amount)
            {
                SpawnMeteorite();
                unlock_time_score = true;
            }

            for (int i = 0; i < brains_amount; i++)
            {
                if (!players[i].dead)
                {
                    outputs = players[i].b.Guess(FetchInputs(i));
                    players[i].Move(outputs);
                }
            }
            UpdatePlayfield();
            UpdateColisions();
            for (int i = 0; i < brains_amount; i++)
            {
                if (brains[i].to_eliminate)
                {
                    brains[i] = new Brain(inputs_amount, outputs_amount);
                }
            }
            if (unlock_time_score)
            {
                current_score += .1;
            }
        }

        /// <summary>
        /// Checks if all Players are dead
        /// </summary>
        /// <returns>true if all players are dead, else false</returns>
        private bool AllDead()
        {
            foreach (Player p in players) if (!p.dead) return false;
            return true;
        }

        /// <summary>
        /// Spawns a new Meteorite at a random position to the left or at the top of the canvas.
        /// </summary>
        private void SpawnMeteorite()
        {
            int type = r.Next(5);
            type = (type < 4) ? -1 : 1;
            double diameter = (int)((double)r.Next(80, 100) / 100 * textures[1].Width);
            int x_or_y = r.Next(2); // 0 = left border, 1 = top border
            double x_pos;
            double y_pos;
            if (x_or_y == 0)
            {
                x_pos = -diameter / 2;
                y_pos = r.Next(height) / 2;
            }
            else
            {
                x_pos = r.Next(width) / 2;
                y_pos = -diameter / 2;
            }

            double rotation = r.Next(280, 350);
            double x_speed = Math.Cos((Math.PI / 180) * rotation);
            double y_speed = -Math.Sin((Math.PI / 180) * rotation);

            debris.Add(new Meteorite(x_pos, y_pos, x_speed, y_speed, type, diameter, width, height));
        }

        /// <summary>
        /// Updates the players and the meteorites positions.
        /// Removes all meteorites off screen meteorites and sets dead = true if the player touches a grey meteorite.
        /// </summary>
        private void UpdatePlayfield()
        {
            List<Meteorite> to_remove = new List<Meteorite>();
            foreach (Meteorite m in debris)
            {
                if (!m.Update())
                {
                    to_remove.Add(m);
                }
            }
            foreach (Player p in players)
            {
                p.Update();
            }
            foreach (Meteorite m in to_remove)
            {
                debris.Remove(m);
                current_score += 1;
            }
        }

        /// <summary>
        /// Checks for colisions.
        /// If player hits a grey meteorite: dead = true.
        /// If player hits a gold meteorite: IncrementScore() and meteorite gets removed.
        /// </summary>
        private void UpdateColisions()
        {
            List<Meteorite> to_remove = new List<Meteorite>();
            foreach (Meteorite m in debris)
            {
                foreach (Player p in players)
                {
                    if (!p.dead && m.GetDistance(p.GetX(), p.GetY()) <= Math.Pow((m.GetD() / 2 + p.GetD() / 2), 2))
                    {
                        if (m.GetT() == 1)
                        {
                            to_remove.Add(m);
                            p.b.Score += gold_meteorite_points;
                        }
                        else
                        {
                            p.dead = true;
                            p.b.Score += current_score;
                        }
                    }
                    if (!p.dead && p.TouchesWall())
                    {
                        p.dead = true;
                        p.b.Score += current_score;
                    }
                }
            }
            foreach (Meteorite m in to_remove)
            {
                debris.Remove(m);
            }
        }

        /// <summary>
        /// Increments the score by 1.
        /// </summary>
        private void IncrementScore()
        {
            current_score += 1;
        }

        /// <summary>
        /// Gets the necessary inputs for the brain.
        /// Saves the results into the list inputs.
        /// </summary>
        private List<double> FetchInputs(int index)
        {
            List<double> degrees = new List<double>() { 0, 45, 90, 180, 225, 270, 315, 338 };
            List<double> inputs = new List<double>();

            for (int i = 0; i < degrees.Count; i++)
            {
                (double distance, int type) = ScanBeam(players[index].GetR() + degrees[i], index);
                //inputs.Add(distance);
                //inputs.Add(type);
                inputs.Add(type * (this.scanbeam_length - distance));
            }
            inputs.Add(players[index].GetR());
            return inputs;
        }

        /// <summary>
        /// Scans for meteorites along a line from the players position with the angle rotation.
        /// </summary>
        /// <param name="rotation">angle of the scanline</param>
        /// <returns>Touple with distance and type of nearest meteorite, if no meteorite was found distance to wall, if wall not in scanbeam_length -> (scanbeam_length + 10, 0)</returns>
        private (double, int) ScanBeam(double rotation, int index)
        {
            double start_x = players[index].GetX();
            double start_y = players[index].GetY();
            double vec_x = Math.Cos((Math.PI / 180) * rotation);
            double vec_y = Math.Sin((Math.PI / 180) * rotation);
            double m_x;
            double m_y;
            List<double> distances = new List<double>();
            List<Meteorite> Ms = new List<Meteorite>();

            foreach (Meteorite m in debris)
            {
                m_x = m.GetX();
                m_y = m.GetY();

                m_x -= start_x;
                m_y -= start_y;

                if (Math.Abs((m_x * vec_y - m_y * vec_x)) / Math.Sqrt(Math.Pow(vec_x, 2) + Math.Pow(vec_y, 2)) < m.GetD())
                {
                    distances.Add(m.GetDistance(start_x, start_y));
                    Ms.Add(m);
                }
            }
            if (distances.Count > 0)
            {
                double min_distance = distances.Min();
                if (min_distance > scanbeam_length)
                    return (scanbeam_length + 100, 0);
                return (min_distance, Ms[distances.IndexOf(distances.Min())].GetT());
            }
            else
            {
                for (int i = 0; i < scanbeam_length; i++)
                {
                    double test_x = start_x + vec_x * i;
                    double test_y = start_y + vec_y * i;

                    if (test_x < 0 || test_x >= width || test_y < 0 || test_y >= height)
                    {
                        return (Math.Pow(i, 2), -1);
                    }
                }
                return (scanbeam_length + 100, 0);
            }
        }

        /// <summary>
        /// Resets the game to its initial state.
        /// </summary>
        private void ResetGame()
        {
            players = new Player[brains_amount];
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = new Player(width / 2, height / 2, textures[0].Width, width, height, brains[i]);
            }
            debris.Clear();
            current_score = 0;
            outputs.Clear();
            scanbeam_length = 300;
            unlock_time_score = false;
        }

        #endregion

        class Player
        {
            double x;
            double y;
            public double x_speed;
            public double y_speed;
            double rotation;
            double d;

            double steer;
            double gas;

            int field_width;
            int field_height;

            public Brain b;
            public bool dead;


            public Player(double x, double y, double diameter, int w, int h, Brain b)
            {
                this.x = x;
                this.y = y;
                x_speed = 0;
                y_speed = 0;
                rotation = 0;
                d = diameter;

                steer = 0;
                gas = 0;

                field_width = w;
                field_height = h;

                this.b = b;
                dead = false;
            }

            /// <summary>
            /// Updates the players position.
            /// </summary>
            public void Update()
            {
                rotation += steer;
                if (rotation < 0) rotation = 360 + rotation;
                rotation %= 360;
                x_speed = Math.Cos(ToRadians(rotation)) * gas;
                y_speed = Math.Sin(ToRadians(rotation)) * gas;
                x += x_speed;
                y += y_speed;
                if (x + d / 2 > field_width) x = field_width - d / 2;
                if (x - d / 2 < 0) x = d / 2;
                if (y + d / 2 > field_height) y = field_height - d / 2;
                if (y - d / 2 < 0) y = d / 2;
            }

            /// <summary>
            /// Sets steer and gas according to the brains outputs.
            /// </summary>
            /// <param name="param">output of brain</param>
            public void Move(List<double> param)
            {
                steer = param[0] * 3 - 1.5;
                gas = param[1] * 3 - 1.5;
            }

            public double GetX() { return x; }
            public double GetY() { return y; }
            public double GetD() { return d; }
            public double GetR() { return rotation; }

            #region functions for manual steering

            public void SteerRight()
            {
                steer += .1;
            }
            public void SteerLeft()
            {
                steer -= .1;
            }
            public void GasUp()
            {
                gas += .01;
            }
            public void GasDown()
            {
                gas -= .01;
            }

            #endregion

            /// <summary>
            /// Converts degrees to radians.
            /// </summary>
            /// <param name="deg">angle in degrees</param>
            /// <returns>angle converted to radians</returns>
            private double ToRadians(double deg) { return (Math.PI / 180) * deg; }

            public bool TouchesWall()
            {
                if (x - d / 2 <= 0) return true;
                if (x + d / 2 >= field_width) return true;
                if (y - d / 2 <= 0) return true;
                if (y + d / 2 >= field_height) return true;
                return false;
            }
        }

        class Meteorite
        {
            double x;
            double y;
            double x_speed;
            double y_speed;
            double d;
            int type;  // -1 = Meteorite; 1 = Gold
            int field_width;
            int field_height;

            public Meteorite(double x, double y, double xspeed, double yspeed, int type, double diameter, int w, int h)
            {
                this.x = x;
                this.y = y;
                x_speed = xspeed;
                y_speed = yspeed;
                this.type = type;
                d = diameter;

                field_width = w;
                field_height = h;
            }

            /// <summary>
            /// Updates the meteorites position
            /// </summary>
            /// <returns>false if meteorite exited the canvas, else true</returns>
            public bool Update()
            {
                x += x_speed;
                y += y_speed;

                if (x > field_width || y > field_height)
                    return false;
                return true;
            }

            public double GetX() { return x; }
            public double GetY() { return y; }
            public double GetD() { return d; }
            public int GetT() { return type; }

            /// <summary>
            /// Calculates the distance to recieved position.
            /// </summary>
            /// <param name="nx">x-position of second object</param>
            /// <param name="ny">y-position of second object</param>
            /// <returns>distance to given coordinates</returns>
            public double GetDistance(double nx, double ny)
            {
                return Math.Pow((x - nx), 2) + Math.Pow((y - ny), 2);
            }
        }

        #endregion

        #region NEAT functions

        /// <summary>
        /// Executes the evolution process and increments the generation number.
        /// </summary>
        private void NextGeneration()
        {
            Console.WriteLine("The best Score was " + GetBestScore() + " in Generation " + generation);

            Console.WriteLine("Max Connections: " + brains.Select(x => x.GetConnectionsCount()).Max());

            List<List<Brain>> categorisedBrains = Speciation();
            int[] distribution = GetDistribution(categorisedBrains);
            categorisedBrains = SortByScore(categorisedBrains);
            categorisedBrains = Elimination(categorisedBrains);
            Evolve(categorisedBrains, distribution);
            generation += 1;
        }

        /// <summary>
        /// Gets the best score of all brains.
        /// </summary>
        /// <returns>best score amongst the brains</returns>
        private double GetBestScore()
        {
            if (brains == null) return -1;
            return brains.Select(x => x.Score).Max();
        }

        #region Mutation functions

        /// <summary>
        /// Creates a new list with sublists for each calculated species.
        /// </summary>
        /// <returns>list of all species</returns>
        private List<List<Brain>> Speciation()
        {
            List<List<Brain>> species = new List<List<Brain>>();
            species.Add(new List<Brain>());
            bool first = true;
            foreach (Brain b in brains)
            {
                if (first)
                {
                    species[0].Add(b);
                    first = false;
                    continue;
                }

                bool skip = false;

                for (int i = 0; i < species.Count; i++)
                {
                    if (species[i][0].EqualStructure(b))
                    {
                        species[i].Add(b);
                        skip = true; // skip is for jumping out of outer loop
                        break;
                    }
                }

                if (!skip)
                {
                    species.Add(new List<Brain>());
                    species[^1].Add(b);
                }
            }
            foreach (List<Brain> s in species)
            {
                int size = s.Count;
                foreach (Brain b in s)
                {
                    b.Score /= size;
                }
            }
            return species;
        }

        /// <summary>
        /// Eliminates lower half of species
        /// </summary>
        /// <param name="species">List of all species"</param>
        /// <returns>decimated species list</returns>
        private List<List<Brain>> Elimination(List<List<Brain>> species)
        {
            for (int i = 0; i < species.Count; i++)
            {
                if (species[i].Count < 4) continue;
                int threshold = species[i].Count / 2;
                species[i] = species[i].GetRange(0, threshold);
            }
            return species;
        }

        /// <summary>
        /// Sorts brains in species by score.
        /// </summary>
        /// <param name="species">list of all species</param>
        /// <returns>new list with sorted species</returns>
        private List<List<Brain>> SortByScore(List<List<Brain>> species)
        {
            for (int i = 0; i < species.Count; i++)
            {
                species[i].Sort((x, y) => x.Score.CompareTo(y.Score));
            }
            return species;
        }

        /// <summary>
        /// Calculates the amount of brains a species will get to reproduce
        /// </summary>
        /// <param name="species">List of the species</param>
        /// <returns>distribution array</returns>
        private int[] GetDistribution(List<List<Brain>> species)
        {
            double[] distr = new double[species.Count];
            int[] result = new int[species.Count];
            double total_score = 0;
            foreach (var s in species)
            {
                foreach (var brain in s)
                {
                    total_score += brain.Score;
                }
            }

            if (total_score == 0) return species.Select(x => x.Count).ToArray();

            for (int i = 0; i < species.Count; i++)
            {
                foreach (Brain b in species[i])
                {
                    distr[i] += b.Score / total_score * brains_amount;
                }
            }
            for (int i = 0; i < distr.Length; i++)
            {
                result[i] = (int)Math.Ceiling(distr[i]);
            }
            if (result.Sum() != brains_amount)
            {
                result[Array.IndexOf(result, result.Max())] += brains_amount - result.Sum();
            }
            if (result.Sum() != brains_amount)
            {
                throw new Exception("Faulty amount of brains to generate calculated!");
            }
            return result;
        }

        /// <summary>
        /// Mutates each brain based on probability parameters
        /// </summary>
        /// <param name="species">list of all species</param>
        private void Evolve(List<List<Brain>> species, int[] distribution)
        {
            brains.Clear();
            for (int i = 0; i < species.Count; i++)
            {
                while (distribution[i] > 0)
                {
                    for (int j = 0; j < species[i].Count; j++)
                    {
                        if (distribution[i] == 0) break;
                        Brain b = species[i][j];

                        if (species[i].Count > 5 && j == 0)
                        {
                            brains.Add(b);
                            continue;
                        }

                        if (species[i].Count > 1 && j != species[i].Count - 1)
                        {
                            var selected_brain = species[i][j + 1];
                            if (r.NextDouble() < species_crossover)
                            {
                                int selected_species = r.Next(species.Count);
                                selected_brain = species[selected_species][r.Next(species[selected_species].Count)];
                            }
                            b = Crossover(species[i][j], selected_brain);
                        }

                        if (r.NextDouble() < mutation)
                        {
                            Mutate(b);
                        }
                        brains.Add(b);
                        distribution[i] -= 1;
                    }
                }
            }
        }

        /// <summary>
        /// Crossover of two brains.
        /// </summary>
        /// <param name="b1">brain 1</param>
        /// <param name="b2">brain 2</param>
        /// <returns>new brain created through the crossover of the two given brains</returns>
        private Brain Crossover(Brain b1, Brain b2)
        {
            // 0.75 => disabled gets inherited
            List<(Brain b, List<int> connections_innov)> parents = new List<(Brain, List<int>)>() { (b1, b1.GetInnovArray()), (b2, b2.GetInnovArray()) };
            int fitter = (b1.GetFittness() > b2.GetFittness()) ? 0 : 1;

            List<(int i, double w, int o, bool e, int innov)> offspring_connections = new List<(int i, double w, int o, bool e, int innov)>();

            foreach (var conn in global_connections)
            {
                int inx = -1;
                if (parents[0].connections_innov.Contains(conn.innov) && parents[1].connections_innov.Contains(conn.innov))
                {
                    // matching connection
                    inx = (r.NextDouble() < .5) ? 0 : 1;
                }
                else if (!parents[fitter].connections_innov.Contains(conn.innov) && parents[Math.Abs(fitter - 1)].connections_innov.Contains(conn.innov))
                {
                    // unique connection in worse brain
                    inx = Math.Abs(fitter - 1);
                }
                else if (parents[fitter].connections_innov.Contains(conn.innov))
                {
                    // unique connection in fitter brain
                    inx = fitter;
                }
                else
                {
                    continue;
                }
                var connection = parents[inx].b.GetConnection(conn.innov);
                if (!connection.Item4 && r.NextDouble() < inherit_disabled)
                {
                    connection = (connection.Item1, connection.Item2, connection.Item3, true, connection.Item5);
                }
                if (connection.Item5 == conn.innov)
                    offspring_connections.Add(connection);
            }

            Brain offspring = new Brain(inputs_amount, outputs_amount, offspring_connections);
            return offspring;
        }

        /// <summary>
        /// Mutates given brain based on probability parameters.
        /// </summary>
        /// <param name="b">brain to mutate</param>
        private void Mutate(Brain b)
        {
            if (r.NextDouble() < node_mutation)
            {
                int innov = GetRandomConnection(b);
                AddNode(innov, b);
            }
            else if (r.NextDouble() < connection_mutation)
            {
                (int i, int o) = b.GetNewConnection();
                if (i < 0 || o < 0) return;
                AddConnection(i, o, b);
            }
            else if (r.NextDouble() < weight_mutation)
            {
                b.MutateWeights();
            }
        }

        /// <summary>
        /// Fills global_nodes and global_connections with initial values based on inputs_amount and outputs_amount.
        /// </summary>
        public void SetupInitialGenome()
        {
            for (int i = 0; i < inputs_amount + outputs_amount; i++)
            {
                global_nodes.Add(i);
            }
            for (int i = 0; i < inputs_amount; i++)
            {
                for (int o = inputs_amount; o < inputs_amount + outputs_amount; o++)
                {
                    global_connections.Add((i, o, global_connections.Count));
                }
            }
        }

        /// <summary>
        /// Adds a connection to the list global_connections.
        /// </summary>
        /// <param name="from">node index of connection start</param>
        /// <param name="to">node index of connection end</param>
        /// <param name="b">brain which should get the connection</param>
        public void AddConnection(int from, int to, Brain b)
        {
            if (from >= global_nodes.Count || to >= global_nodes.Count)
            {
                throw new ArgumentException("Node not found!");
            }
            int innov;
            if (!ConnectionExists(from, to))
            {
                innov = global_connections.Count;
                global_connections.Add((from, to, global_connections.Count));
            }
            else
            {
                innov = GetConnectionInnov(from, to);
                if (innov < 0) throw new ArgumentException("Connection couldn't be found although ConnectionExists returned true");
            }

            if (b != null)
            {
                b.AddConnection(from, to, innov);
            }
        }

        /// <summary>
        /// Adds a Node between given nodes and creates new connections accordingly.
        /// </summary>
        /// <param name="from">node index of connection start</param>
        /// <param name="to">node index of connection end</param>
        /// <param name="b">brain to recieve this node</param>
        public void AddNode(int innov, Brain b)
        {
            global_nodes.Add(global_nodes.Count);
            AddConnection(global_connections[innov].i, global_nodes.Count - 1, null);
            AddConnection(global_nodes.Count - 1, global_connections[innov].o, null);
            b.AddNode(innov, global_connections.Count - 2, global_connections.Count - 1, global_nodes.Count - 1);
        }

        #region helper functions

        /// <summary>
        /// Gets a random connection.
        /// </summary>
        /// <returns>random connection innovation number</returns>
        private int GetRandomConnection(Brain b)
        {
            if (b != null)
            {
                return b.GetConnections()[r.Next(b.GetConnections().Length)];
            }
            return global_connections[r.Next(global_connections.Count)].innov;
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
            foreach (var conn in global_connections)
            {
                if (conn.i == i && conn.o == o) return true;
                if (conn.o == i && conn.i == o) return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the innovation number of given connection.
        /// </summary>
        /// <param name="from">node index of connection start</param>
        /// <param name="to">node index of connection end</param>
        /// <returns>innovation number of connection, if not found -1</returns>
        public int GetConnectionInnov(int from, int to)
        {
            foreach (var c in global_connections)
            {
                if (c.i == from && c.o == to || c.o == from && c.i == to) return c.innov;
            }
            return -1;
        }

        #endregion

        #endregion

        #endregion
    }
}



