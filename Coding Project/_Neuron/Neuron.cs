using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _Neuron
{
    public class Neuron
    {
        int[] iteration_helper;

        Random r;
        double[] inputs;
        double[] weights;
        double learning_rate;

        public double LearningRate
        {
            get { return learning_rate; }
            set { learning_rate = value; }
        }

        /// <summary>
        /// Initialises the neuron.
        /// </summary>
        /// <param name="input_amount">Amount of expected inputs</param>
        public Neuron(int input_amount, double learning_rate)
        {
            r = new Random();
            input_amount += 1; // for bias
            this.learning_rate = learning_rate;
            inputs = new double[input_amount];
            weights = new double[input_amount];
            iteration_helper = new int[input_amount];
            for (int i = 0; i < input_amount; i++)
            {
                weights[i] = r.NextDouble();
                iteration_helper[i] = i;
            }
        }

        /// <summary>
        /// Takes a guess based on given inputs.
        /// </summary>
        /// <param name="ins">Input array (bias excluded)</param>
        /// <returns>Input array (bias included) if result == 1, if not then null</returns>
        public (double[], double) Guess(double[] ins)
        {
            SetInputs(ins);
            double result = iteration_helper.Select(x => inputs[x] * weights[x]).Sum();
            result = Activate(result);
            if (result > 0)
                return (inputs, result);
            else
                return (null, result);
        }

        /// <summary>
        /// Adjusts the weights based on the difference between target and result.
        /// </summary>
        /// <param name="target">Value the neuron should have calculated</param>
        /// <param name="result">Value the neuron calculated</param>
        /// <param name="ins">Input array the neuron should use for training (output of Guess)</param>
        /// <returns>True if training was successful, if not then false</returns>
        public bool Train(double target, double result, double[] ins)
        {
            double error = target - result;
            if (error == 0) return true;
            double[] deltaW = ins.Select(x => learning_rate * x * error).ToArray();
            weights = iteration_helper.Select(x => weights[x] + deltaW[x]).ToArray();
            return true;
        }

        /// <summary>
        /// Sets every weight to new random double
        /// </summary>
        public void RegenerateWeights()
        {
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = r.NextDouble();
            }
        }

        /// <summary>
        /// Copys values of ins to global array inputs and prepends the bias.
        /// </summary>
        /// <param name="ins">Inputs</param>
        /// <returns>True if inputs were properly set, false if the length was invalid</returns>
        private bool SetInputs(double[] ins)
        {
            if (ins.Length + 1 != inputs.Length)
                return false;

            inputs = ins.Prepend(1).ToArray();
            return true;
        }

        /// <summary>
        /// Activation function.
        /// </summary>
        /// <param name="val">Value to be activated</param>
        /// <returns>Activated value</returns>
        private static double Activate(double val)
        {
            #region linear activation

            //return val;

            #endregion

            #region binary activation

            if (val > 0)
                return 1;
            else
                return 0;

            #endregion

            #region tanh activation

            //return Math.Tanh(val);

            #endregion

            #region sigmoid activation

            //return .5 * (1 + Math.Tanh(val / 2));

            #endregion
        }

        /// <summary>
        /// Saves the current amount of inputs and the current weights in a textfile located in the Content folder.
        /// </summary>
        /// <returns>true if write was successful, false if not</returns>
        public bool Save()
        {
            using (StreamWriter sw = File.CreateText(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString()).ToString()) + "/Content/saved_neuron.txt"))
            {
                sw.Write("input_amount : " + inputs.Length + ";\n");
                for (int i = 0; i < weights.Length; i++)
                {
                    sw.Write("weight " + i + " : " + weights[i] + ";\n");
                }
            }
            return true;
        }

        /// <summary>
        /// Loads amount of inputs and weights from the textfile in the Content folder.
        /// </summary>
        /// <returns>true if load was successful, else false</returns>
        public bool Load()
        {
            string text = File.ReadAllText(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString()).ToString()) + "/Content/saved_neuron.txt");
            string[] sub = text.Split(";").Select(x => x.Remove(0, x.IndexOf(":") + 2)).SkipLast(1).ToArray();
            double[] values = sub.Select(x => double.Parse(x)).ToArray();
            if (values != null && values.Length > 0 && values[0] == inputs.Length)
            {
                weights = values.Skip(1).ToArray();
            }
            return true;
        }

        /// <summary>
        /// Returns the weights array
        /// </summary>
        /// <returns>weights</returns>
        public double[] GetWeights()
        {
            return weights;
        }
    }
}
