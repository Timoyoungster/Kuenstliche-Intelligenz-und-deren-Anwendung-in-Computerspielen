using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _Neuron
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont font;

        Runner r;
        Random random;
        Neuron p;
        List<Piece> cacti;
        int width;
        int height;
        float speed;
        double[] input_save;
        int score;
        int highscore;
        int generation;
        bool saved;
        bool loaded;
        bool learning_rate_changed;
        float jumplength;
        double init_learning_rate;

        int test_iterations;
        int target_score;
        int test_iteration_generations;
        int current_iteration;
        string logfilepath;
        List<List<int>> log_array;

        // Textures
        List<Texture2D> unique_pieces;
        Texture2D knight;


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        #region - monogame functions -

        protected override void Initialize()
        {
            init_learning_rate = .00001;
            p = new Neuron(3, init_learning_rate);
            random = new Random();
            cacti = new List<Piece>();
            speed = 5;
            score = 0;
            generation = 0;
            width = GraphicsDevice.Viewport.Width;
            height = GraphicsDevice.Viewport.Height;
            unique_pieces = new List<Texture2D>();
            saved = false;
            loaded = false;
            learning_rate_changed = false;
            test_iterations = 100;
            current_iteration = 0;
            target_score = 4800;
            test_iteration_generations = 120;
            log_array = new List<List<int>>();
            for (int i = 0; i < test_iteration_generations; i++) log_array.Add(new List<int>());
            logfilepath = "/Users/timoyoungster/Projects/VWA/Neuron_Logs.txt";
            _graphics.GraphicsDevice.Clear(Color.White);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("Font");
            unique_pieces.Add(Content.Load<Texture2D>("Bishop"));
            unique_pieces.Add(Content.Load<Texture2D>("Pawn"));
            knight = Content.Load<Texture2D>("Knight");
            r = new Runner(knight, new Vector2(50, height - knight.Height));
            jumplength = r.GetPosition().X + 60 * speed + r.GetTexture().Width;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            var kstate = Keyboard.GetState();
            if (kstate.IsKeyDown(Keys.P))
                while (!UpdateNeuronGame())
                    Exit();
            if (kstate.IsKeyDown(Keys.U))
                for (int i = 0; i < 100; i++)
                    if (UpdateNeuronGame()) break;
            if (kstate.IsKeyDown(Keys.I))
                for (int i = 0; i < 1000; i++)
                    if (UpdateNeuronGame()) break;
            if (kstate.IsKeyDown(Keys.O))
                for (int i = 0; i < 10000; i++)
                    if (UpdateNeuronGame()) break;
            if (!kstate.IsKeyDown(Keys.H) && !kstate.IsKeyDown(Keys.Z) && learning_rate_changed)
            {
                learning_rate_changed = false;
            }
            else if (kstate.IsKeyDown(Keys.H) && !learning_rate_changed)
            {
                p.LearningRate /= 10;
                learning_rate_changed = true;
            }
            else if (kstate.IsKeyDown(Keys.Z) && !learning_rate_changed)
            {
                p.LearningRate *= 10;
                learning_rate_changed = true;
            }
            if (kstate.IsKeyDown(Keys.Up))
                IncrementSpeed();
            if (kstate.IsKeyDown(Keys.Down))
                IncrementSpeed();
            if (kstate.IsKeyDown(Keys.S))
                if (p.Save())
                    saved = true;
            if (kstate.IsKeyDown(Keys.L))
                if (p.Load())
                    loaded = true;
            if (kstate.IsKeyDown(Keys.R))
                p.RegenerateWeights();
            UpdateNeuronGame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            _spriteBatch.Begin();
            _spriteBatch.Draw(r.GetTexture(), r.GetPosition(), Color.White);
            List<Piece> dangerous_cacti = cacti.Select(x => x).Where(x => x.GetPosition().X > (int)r.GetPosition().X).ToList();
            Piece firstcactus_X = null;
            if (dangerous_cacti.Count > 0)
            {
                firstcactus_X = dangerous_cacti.Select(x => x).ToArray()[0];
            }
            foreach (Piece p in cacti)
            {
                if (firstcactus_X == p)
                    _spriteBatch.Draw(p.GetTexture(), p.GetPosition(), Color.Red);
                else
                    _spriteBatch.Draw(p.GetTexture(), p.GetPosition(), Color.White);
            }
            if (r.EarlyJump())
                _spriteBatch.DrawString(font, "Early Jump!", new Vector2(20, 120), Color.Red);
            _spriteBatch.DrawString(font, "Score: " + score, new Vector2(20, 20), Color.Black);
            _spriteBatch.DrawString(font, "Highscore: " + highscore, new Vector2(20, 45), Color.Black);
            _spriteBatch.DrawString(font, "Generation: " + generation, new Vector2(20, 70), Color.Black);
            _spriteBatch.DrawString(font, "Learning Rate: " + p.LearningRate, new Vector2(20, 95), Color.Black);
            _spriteBatch.DrawString(font, "Iteration: " + current_iteration, new Vector2(20, 145), Color.Black);
            if (saved)
                _spriteBatch.DrawString(font, "Neuron state saved!", new Vector2(width - 200, 20), Color.Green);
            else if (loaded)
                _spriteBatch.DrawString(font, "Neuron state loaded!", new Vector2(width - 200, 20), Color.Green);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        #endregion

        #region - neuron game functions -

        /// <summary>
        /// Calculates neuron output and updates the game accordingly.
        /// If player dies the neuron gets trained.
        /// </summary>
        private bool UpdateNeuronGame()
        {

            if (generation == test_iteration_generations)
            {
                ResetGame();
                Reset();
                current_iteration += 1;
            }
            if (current_iteration == test_iterations)
            {
                LogAll();
                return true;
            }

            if (r.GetPosition().Y == height - r.GetTexture().Height && !r.EarlyJump())
            {
                input_save = null;
                r.SetJumpingFlag(false);
            }
            (double[] ins, double result) = p.Guess(FetchInputs());
            if (ins != null)
            {
                if (!r.FlagSet())
                {
                    input_save = (double[])ins.Clone();
                    r.Jump(ins[1], speed);
                }
            }
            r.Update();
            if (UpdateCacti())
            {
                if (input_save != null)
                    p.Train(GetTarget(), result, input_save);
                else
                    p.Train(GetTarget(), result, FetchInputs().Prepend(1).ToArray());
                if (score > highscore)
                    highscore = score;
                Reset();
                //return true;
            }
            if (score >= target_score)
            {
                Reset();
                //return true;
            }
            return false;
        }

        /// <summary>
        /// Spawns the next cactus just outside the canvas.
        /// </summary>
        /// <returns>true if spawned successful, else false</returns>
        private bool SpawnCactus()
        {
            Texture2D tex = unique_pieces[random.Next(unique_pieces.Count)];
            cacti.Add(new Piece(tex, new Vector2(width, height - tex.Height)));
            return true;
        }

        /// <summary>
        /// Updates the Cacti's position and checks for colision.
        /// </summary>
        /// <returns>false if no colision detected, true if player dead</returns>
        private bool UpdateCacti()
        {
            for (int i = 0; i < cacti.Count; i++)
            {
                cacti[i].Move(speed);

                if (r.GetPosition().X + r.GetWidth() > cacti[i].GetPosition().X && r.GetPosition().X < cacti[i].GetPosition().X + cacti[i].GetTexture().Width)
                    if (r.GetPosition().Y + r.GetTexture().Height >= height - cacti[i].GetTexture().Height)
                        return true;

                if (cacti[i].GetPosition().X + cacti[i].GetTexture().Width < 0)
                {
                    cacti.RemoveAt(i);
                    score += 1;
                    if (score % 50 == 0 && score != 0)
                        IncrementSpeed();
                    i -= 1;
                }
            }

            if (cacti.Count == 0 || cacti.Select(x => x.GetPosition().X).Max() < width - jumplength)
            {
                if (random.Next(100) == 1)
                    SpawnCactus();
            }
            return false;
        }

        /// <summary>
        /// Increments the speed by .1 and updates jumplength.
        /// </summary>
        private void IncrementSpeed()
        {
            speed += .1f;
            jumplength = r.GetPosition().X + 60 * speed + r.GetTexture().Width;
        }

        /// <summary>
        /// Collects the inputs for the neuron.
        /// </summary>
        /// <returns>double array with collected inputs</returns>
        private double[] FetchInputs()
        {

            double firstcactus_X;
            double firstcactus_H;

            if (cacti.Count == 0)
            {
                firstcactus_X = width;
                firstcactus_H = 50;
            }
            else
            {
                List<Piece> dangerous_cacti = cacti.Select(x => x).Where(x => x.GetPosition().X > (int)r.GetPosition().X).ToList();
                if (dangerous_cacti.Count > 0)
                {
                    firstcactus_X = dangerous_cacti.Select(x => x.GetPosition().X).ToArray()[0];
                    firstcactus_H = dangerous_cacti.Select(x => x).Where(x => x.GetPosition().X == firstcactus_X).ToArray()[0].GetTexture().Height;
                }
                else
                {
                    firstcactus_X = width;
                    firstcactus_H = 50;
                }
            }

            return new double[] { firstcactus_X, firstcactus_H, speed };
        }

        /// <summary>
        /// Calculates the target value for neuron training.
        /// </summary>
        /// <returns>target value</returns>
        private double GetTarget()
        {
            if (r.EarlyJump())
                return -1;
            if (input_save == null)
                return 1;
            if (r.GetYSpeed() > 0)
                return 1;
            else
                return -1;
        }

        /// <summary>
        /// Resets the game to its starting state.
        /// </summary>
        private void Reset()
        {
            LogRun();
            r.Reset(knight, new Vector2(50, height - knight.Height));
            generation += 1;
            score = 0;
            input_save = null;
            speed = 5;
            cacti.Clear();
            saved = false;
            loaded = false;
        }

        /// <summary>
        /// Resets everything to starting position
        /// also recreates Neuron
        /// </summary>
        private void ResetGame()
        {
            p = new Neuron(3, init_learning_rate);
            r.Reset(knight, new Vector2(50, height - knight.Height));
            generation = 0;
            score = 0;
            input_save = null;
            speed = 5;
            cacti.Clear();
            saved = false;
            loaded = false;
        }

        /// <summary>
        /// Writes score of current run
        /// </summary>
        private void LogRun()
        {
            if (generation < log_array.Count) log_array[generation].Add(score);
        }

        /// <summary>
        /// writes Log-Array to File
        /// </summary>
        private void LogAll()
        {
            File.WriteAllText(logfilepath, PrintList(log_array));
        }

        public string PrintList(List<List<int>> l)
        {
            string result = "";
            foreach (List<int> gen in l)
            {
                foreach (int run in gen)
                {
                    result += run.ToString();
                    result += ";";
                }
                result += "\n";
            }
            return result;
        }

        /// <summary>
        /// Returns input double array as string
        /// </summary>
        /// <param name="arr">double array to convert to string</param>
        /// <returns>array as string</returns>
        private string PrintArray(Double[] arr)
        {
            string result = "";
            foreach (double elem in arr)
            {
                result += elem.ToString() + ", ";
            }
            return result;
        }

        #endregion
    }

    public class Piece
    {
        private Texture2D texture;
        private Vector2 position;

        public Piece(Texture2D tex, Vector2 vec)
        {
            texture = tex;
            position = vec;
        }

        public Vector2 GetPosition() { return position; }
        public Texture2D GetTexture() { return texture; }
        public void Move(float speed) { position.X -= speed; }
    }

    public class Runner
    {
        private float gravity;
        private float y_speed;
        private float w;
        private float height;
        private float window_height;
        private bool jumping_flag;
        private bool jumped_too_early_flag;
        private Texture2D texture;
        private Vector2 position;

        public Runner(Texture2D tex, Vector2 vec)
        {
            height = tex.Height;
            window_height = vec.Y;
            jumping_flag = false;
            jumped_too_early_flag = false;
            y_speed = 0;
            gravity = -.3f;
            w = tex.Width;
            texture = tex;
            position = vec;
        }

        /// <summary>
        /// Initiates runner jumping.
        /// </summary>
        /// <returns>true if jumpstart successful, else false</returns>
        public bool Jump(double firstcactus_X, float speed)
        {
            if (jumping_flag) return false;
            if (firstcactus_X > position.X + 60 * speed + texture.Width)
                jumped_too_early_flag = true;
            y_speed = 9f;
            jumping_flag = true;
            return true;
        }

        /// <summary>
        /// Updates the runners altitude.
        /// </summary>
        /// <returns>true if update successful, else false</returns>
        public bool Update()
        {
            if (position.Y != 0)
                y_speed += gravity;
            position.Y -= y_speed;
            if (position.Y > window_height)
            {
                position.Y = window_height;
                y_speed = 0;
            }
            return true;
        }

        public float GetWidth() { return w; }
        public float GetYSpeed() { return y_speed; }
        public bool FlagSet() { return jumping_flag; }
        public void SetJumpingFlag(bool b) { jumping_flag = b; }
        public Texture2D GetTexture() { return texture; }
        public Vector2 GetPosition() { return position; }
        public bool EarlyJump() { return jumped_too_early_flag; }

        /// <summary>
        /// Resets the runner to its initial state.
        /// </summary>
        /// <param name="tex">runners texture</param>
        /// <param name="vec">runners initial position</param>
        public void Reset(Texture2D tex, Vector2 vec)
        {
            height = tex.Height;
            window_height = vec.Y;
            jumping_flag = false;
            jumped_too_early_flag = false;
            y_speed = 0;
            gravity = -.3f;
            w = tex.Width;
            texture = tex;
            position = vec;
        }
    }
}
