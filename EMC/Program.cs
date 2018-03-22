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
            (3,3,3,1,1,6,Layer.ActivationFunctions.SoftPlus),
            (3,3,6,2,2,3,Layer.ActivationFunctions.LogSig)
        };

        static int Main(string[] args)
        {
            if (args.Length == 4 && args[0].ToUpperInvariant() == "TRAIN")
                Train(args[1], args[2], int.Parse(args[3]), new Random((int)DateTime.UtcNow.Ticks));
            else if (args.Length == 4 && args[0].ToUpperInvariant() == "RUN")
                Run(args[1], args[2], args[3]);
            else
                Console.WriteLine(args);
            Console.Read();
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

                population.Sort(((Network, double) a, (Network, double) b) => b.Item2.CompareTo(a.Item2));
                Console.WriteLine($",{population[0].Item2},{population[1].Item2}");
                population = population.Take(PASS_SIZE).ToList();


                for (int i = 0; i < POPULATION_SIZE - PASS_SIZE; i++)
                    population.Add((new Network(population[i].Item1, random), double.NaN));
            }

            for (int i = 0; i < population.Count; i++)
                population[i].Item1.Save(Path.Combine(outputPath, $"{i}.bin"));
        }

        static double Evaluate(Network network, List<Bitmap> references)
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

        static void Run(string networkPath, string inputPath, string outputPath)
        {
            Network network = new Network(networkPath);
            Bitmap small = new Bitmap(inputPath);
            BitmapData smallData = small.LockBits(new Rectangle(new Point(0), small.Size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            double[] smallDoubles = DataToDoubles(smallData);
            small.UnlockBits(smallData);
            double[] expandDoubles = network.Apply(smallDoubles, smallData.Width, smallData.Height, 3);

            int outputWidth = network.GetOutputWidth(smallData.Width),
                outputHeight = network.GetOutputHeight(smallData.Height);
            Bitmap large = new Bitmap(outputWidth, outputHeight, PixelFormat.Format24bppRgb);
            //for (int y = 0; y < outputHeight; y++)
            //    for (int x = 0; x < outputWidth; x++)
            //        large.SetPixel(x, y, Color.FromArgb((byte)(expandDoubles[y * outputWidth * 3 + x * 3] * 255.0f), (byte)(expandDoubles[y * outputWidth * 3 + x * 3 + 1] * 255.0f), (byte)(expandDoubles[y * outputWidth * 3 + x * 3 + 1] * 255.0f)));
            BitmapData largeData = large.LockBits(new Rectangle(new Point(0), large.Size), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            DoublesToData(largeData, expandDoubles);
            large.UnlockBits(largeData);
            large.Save(outputPath);
        }

        unsafe public static double[] DataToDoubles(BitmapData data)
        {
            double[] floatingData = new double[data.Height * data.Width * 3];
            byte* d = (byte*)data.Scan0.ToPointer();
            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    for (int c = 0; c < 3; c++)
                        floatingData[y * data.Width * 3 + x * 3 + c] = d[y * data.Stride + x * 3 + c] / 255.0;
            return floatingData;
        }

        unsafe public static void DoublesToData(BitmapData data, double[] floatingData)
        {
            byte* d = (byte*)data.Scan0.ToPointer();
            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    for (int c = 0; c < 3; c++)
                        d[y * data.Stride + x * 3 + c] = (byte)(Math.Max(0.0, Math.Min(floatingData[y * data.Width * 3 + x * 3 + c], 1.0)) * 255.0);
        }

        public static double ATanh(double x) => Math.Log((1 + x) / (1 - x)) / 2;
    }
}
