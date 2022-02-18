using System;
using System.Collections.Generic;
using System.Linq;

namespace _NEAT
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();

            //Brain b = new Brain(11, 2);
            //b.AddNode(12, 28, 29, 16);
            //b.AddConnection(10, 16, 49);
            //b.AddNode(16, 60, 61, 29);
            //b.AddConnection(4, 16, 71);
            //b.AddNode(29, 72, 73, 33);
            //b.AddConnection(7, 16, 74);
            //b.AddNode(6, 89, 90, 38);
            //b.AddConnection(0, 38, 95);
            //b.AddNode(2, 128, 129, 49);
            //b.AddNode(3, 146, 147, 54);
            //b.AddNode(4, 155, 156, 57);
            //b.AddConnection(38, 54, 157);
            //b.AddNode(9, 196, 197, 69);
            //b.AddNode(28, 198, 199, 70);
            //b.GetNewConnection();
            //b.Guess(new List<double>() { 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 300 });
        }
    }
}
