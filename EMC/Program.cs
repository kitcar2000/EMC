using System;
using System.Collections.Generic;
using System.Drawing.
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMC
{
    class Program
    {
        const int POPULATION_SIZE = 100;
        const int PASS_SIZE = 10;

        static List<(int, int, int, int, int, int, Layer.ActivationFunction)> structure = new List<(int, int, int, int, int, int, Layer.ActivationFunction)> {

        };

        static void Main(string[] args)
        {
            List<(Network, double)> population = new List<(Network, double)>(POPULATION_SIZE);

            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                population[i] = (new Network(structure), double.NaN);
            }

            while (true)
            {
                for (int i = 0; i < POPULATION_SIZE; i++)
                    population[i] = (population[i].Item1, Evaluate(population[i].Item1));

                population.Sort(((Network, double) a, (Network, double) b) => b.Item2.CompareTo(a.Item2));
                List<(Network, double)> survivors = population.Take(PASS_SIZE).ToList();
                // Create new population
            }
        }

        static double Evaluate(Network network)
        {
            // Haha kill me
            throw new Exception();
        }

        public static double ATanh(double x) => Math.Log((1 + x) / (1 - x)) / 2;
    }
}
