using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMC
{
    class Program
    {
        const int POPULATION_SIZE = 10;
        const int PASS_SIZE = 3;

        static List<(int, int, int, int, int, int, Layer.ActivationFunction)> structure = new List<(int, int, int, int, int, int, Layer.ActivationFunction)> {
            (2,2,3,1,1,5,Layer.ActivationFunctions.LogSig),
            (2,2,5,2,2,3,Layer.ActivationFunctions.LogSig)
        };

        static int Main(string[] args)
        {
            //Train("C:\\Users\\User\\Pictures\\物語色", "", 10);
            if (args.Length == 4 && args[0].ToUpperInvariant() == "TRAIN")
                Train(args[1], args[2], int.Parse(args[3]), new Random((int)DateTime.UtcNow.Ticks));
            return 0;
        }

        static void Train(string sampleDirectoryPath, string outputPath, int generations, Random random)
        {
            List<(Network, double)> population = new List<(Network, double)>(POPULATION_SIZE);

            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                Network next = new Network(structure);
                next.Mutate(random);
                population.Add((next, double.NaN));
            }

            Train(population, sampleDirectoryPath, outputPath, generations, random);
        }

        static void Train(List<(Network, double)> population, string sampleDirectoryPath, string outputPath, int generations, Random random)
        {
            List<Bitmap> refs = new List<Bitmap>();
            foreach (FileInfo fi in new DirectoryInfo(sampleDirectoryPath).EnumerateFiles())
                refs.Add(new Bitmap(fi.FullName));

            for (int generation = 0; generation < generations; generation++)
            {
                for (int i = 0; i < POPULATION_SIZE; i++)
                {
                    population[i] = (population[i].Item1, Evaluate(population[i].Item1, refs));
                    Console.Write(population[i].Item2.ToString() + ",");
                }

                population.Sort(((Network, double) a, (Network, double) b) => a.Item2.CompareTo(b.Item2));
                Console.WriteLine($",{population[0].Item2},{population[1].Item2}");
                population = population.Take(PASS_SIZE).ToList();


                for (int i = 0; i < POPULATION_SIZE - PASS_SIZE; i++)
                    population.Add((new Network(population[i].Item1, random), double.NaN));
            }
        }

        unsafe static double Evaluate(Network network, List<Bitmap> references)
        {
            double score = 0.0;
            foreach (Bitmap reference in references)
            {
                Bitmap reduced = new Bitmap(reference, reference.Width / 2, reference.Height / 2);

                BitmapData smallData = reduced.LockBits(new Rectangle(new Point(0), reduced.Size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                double[] expandDoubles = network.Apply(DataToDoubles(smallData), smallData.Width, smallData.Height, smallData.Stride / smallData.Width);
                reduced.UnlockBits(smallData);

                int outputWidth = network.GetOutputWidth(smallData.Width),
                    outputHeight= network.GetOutputHeight(smallData.Height);
                BitmapData largeData = reference.LockBits(new Rectangle((reference.Width - outputWidth) / 2, (reference.Height - outputHeight) / 2, outputWidth, outputHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                double[] largeDoubles = DataToDoubles(largeData);
                reference.UnlockBits(largeData);

                double singleScore = 0.0;
                for (int i = 0; i < largeDoubles.Length; i++)
                    singleScore += Math.Pow(expandDoubles[i] - largeDoubles[i], 2);
                score += singleScore / largeDoubles.Length;
            }
            // Haha kill me
            return -score;  // Negate because big score comes from more wrongness.
        }

        unsafe public static double[] DataToDoubles(BitmapData data)
        {
            int dataSize = data.Stride * data.Height;
            double[] floatingData = new double[data.Height * data.Stride];
            byte* d = (byte*)data.Scan0.ToPointer();
            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    for (int c = 0; c < 3; c++)
                        floatingData[y * data.Height * 3 + x * 3 + c] = d[y*data.Stride+x*3+c];
            return floatingData;
        }

        public static double ATanh(double x) => Math.Log((1 + x) / (1 - x)) / 2;
    }
}
