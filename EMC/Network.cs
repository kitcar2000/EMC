using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMC
{
    class Layer
    {
        public delegate double ActivationFunction(double x);
        public static class ActivationFunctions
        {
            public static double SoftPlus(double x) => Math.Log(Math.Exp(x) + 1);
            public static double ReLU(double x) => x > 0 ? x : 0;
            public static double Tanh(double x) => Math.Tanh(x);
            public static double LogSig(double x) => 1.0 / (Math.Exp(x) + 1.0);
        }

        private int inWidth,
            inHeight,
            inChannels,
            outWidth,
            outHeight,
            outChannels;

        private double[] weights;
        private double[] bias;

        private ActivationFunction f;

        public int InputWidth => this.inWidth;
        public int InputHeight => this.inHeight;
        public int InputChannels => this.inChannels;
        public int InputSize => this.inWidth * this.inHeight * this.inChannels;
        public int OutputWidth => this.outWidth;
        public int OutputHeight => this.outHeight;
        public int OutputChannels => this.outChannels;
        public int OutputSize => this.outWidth * this.outHeight * this.outChannels;
        public ActivationFunction Activation => this.f;

        public Layer(Layer original)
        {
            this.inWidth = original.inWidth;
            this.inHeight = original.inHeight;
            this.inChannels = original.inChannels;
            this.outWidth = original.outWidth;
            this.outHeight = original.outHeight;
            this.outChannels = original.outChannels;

            this.weights = new double[original.InputSize * original.OutputSize];
            this.bias = new double[original.OutputSize];

            original.weights.CopyTo(this.weights, 0);
            original.bias.CopyTo(this.bias, 0);

            this.f = original.f;
        }

        public Layer(Layer parent1, Layer parent2, Random random)
        {
            this.inWidth = parent1.inWidth;
            this.inHeight = parent1.inHeight;
            this.inChannels = parent1.inChannels;
            this.outWidth = parent1.outWidth;
            this.outHeight = parent1.outHeight;
            this.outChannels = parent1.outChannels;

            this.weights = new double[parent1.InputSize * parent1.OutputSize];
            this.bias = new double[parent1.OutputSize];

            for (int i = 0; i < this.weights.Length; i++)
                this.weights[i] = ((random.Next() & 1) == 1) ? parent1.weights[i] : parent2.weights[i];
            for (int i = 0; i < this.bias.Length; i++)
                this.bias[i] = ((random.Next() & 1) == 1) ? parent1.bias[i] : parent2.bias[i];

            this.f = parent1.f;
        }

        public Layer(int inputWidth, int inputHeight, int inputChannels, int outputWidth, int outputHeight, int outputChannels, ActivationFunction f)
        {
            this.inWidth = inputWidth;
            this.inHeight = inputHeight;
            this.inChannels = inputChannels;
            this.outWidth = outputWidth;
            this.outHeight = outputHeight;
            this.outChannels = outputChannels;

            this.weights = new double[inputWidth * inputHeight * inputChannels * outputWidth * outputHeight * outputChannels];
            this.bias = new double[outputWidth * outputHeight * outputChannels];

            this.f = f;
        }

        public Layer(int inputWidth, int inputHeight, int inputChannels, int outputWidth, int outputHeight, int outputChannels, ActivationFunction f, double[] weights, double[] bias)
        {
            this.inWidth = inputWidth;
            this.inHeight = inputHeight;
            this.inChannels = inputChannels;
            this.outWidth = outputWidth;
            this.outHeight = outputHeight;
            this.outChannels = outputChannels;

            this.weights = new double[inputWidth * inputHeight * inputChannels * outputWidth * outputHeight * outputChannels];
            this.bias = new double[outputWidth * outputHeight * outputChannels];
            weights.CopyTo(this.weights, 0);
            bias.CopyTo(this.bias, 0);

            this.f = f;
        }

        public double this[int inputX, int inputY, int inputChannel, int outputX, int outputY, int outputChannel]
        {
            get => this.weights[inputY * this.inWidth * this.inChannels * this.outHeight * this.outWidth * this.outChannels +
                                               inputX * this.inChannels * this.outHeight * this.outWidth * this.outChannels +
                                                           inputChannel * this.outHeight * this.outWidth * this.outChannels +
                                                                                 outputY * this.outWidth * this.outChannels +
                                                                                                 outputX * this.outChannels +
                                                                                                               outputChannel];
            set => this.weights[inputY * this.inWidth * this.inChannels * this.outHeight * this.outWidth * this.outChannels +
                                               inputX * this.inChannels * this.outHeight * this.outWidth * this.outChannels +
                                                           inputChannel * this.outHeight * this.outWidth * this.outChannels +
                                                                                 outputY * this.outWidth * this.outChannels +
                                                                                                 outputX * this.outChannels +
                                                                                                              outputChannel] = value;
        }
        public double this[int outputX, int outputY, int outputChannel]
        {
            get => this.bias[outputY * this.outWidth * this.outChannels +
                                             outputX * this.outChannels +
                                                          outputChannel];
            set => this.bias[outputY * this.outWidth * this.outChannels +
                                             outputX * this.outChannels +
                                                         outputChannel] = value;
        }

        public double[] Apply(double[] input)
        {
            double[] output = new double[this.outWidth * this.outHeight * this.outChannels];
            this.bias.CopyTo(output, 0);
            for (int ix = 0; ix < this.inWidth; ix++)
                for (int iy = 0; iy < this.inHeight; iy++)
                    for (int ic = 0; ic < this.inChannels; ic++)
                        for (int ox = 0; ox < this.outWidth; ox++)
                            for (int oy = 0; oy < this.outHeight; oy++)
                                for (int oc = 0; oc < this.outChannels; oc++)
                                    output[oy * this.outWidth * this.outChannels +
                                                           ox * this.outChannels +
                                                                              oc] += this.weights[iy * this.inWidth * this.inChannels * this.outHeight * this.outWidth * this.outChannels +
                                                                                                                 ix * this.inChannels * this.outHeight * this.outWidth * this.outChannels +
                                                                                                                                   ic * this.outHeight * this.outWidth * this.outChannels +
                                                                                                                                                    oy * this.outWidth * this.outChannels +
                                                                                                                                                                    ox * this.outChannels +
                                                                                                                                                                                       oc] * input[iy * this.inWidth * this.inChannels +
                                                                                                                                                                                                                  ix * this.inChannels +
                                                                                                                                                                                                                                    ic];
            for (int ox = 0; ox < this.outWidth; ox++)
                for (int oy = 0; oy < this.outHeight; oy++)
                    for (int oc = 0; oc < this.outChannels; oc++)
                        output[oy * this.outWidth * this.outChannels +
                                               ox * this.outChannels +
                                                                   oc] = this.f(output[oy * this.outWidth * this.outChannels +
                                                                                                       ox * this.outChannels +
                                                                                                                          oc]);
            return output;
        }
    }
    class Network
    {
        List<Layer> layers;

        public int GetOutputWidth(int inputWidth) => Enumerable.Aggregate(this.layers, inputWidth, (int w, Layer l) => (1 + w - l.InputWidth) * l.OutputWidth);
        public int GetOutputHeight(int inputHeight) => Enumerable.Aggregate(this.layers, inputHeight, (int w, Layer l) => (1 + w - l.InputHeight) * l.OutputHeight);
        public int GetOutputChannels() => this.layers.Last().OutputChannels;
        public int GetInputChannels() => this.layers.First().InputChannels;

        public Network(List<(int, int, int, int, int, int, Layer.ActivationFunction)> layers)
        {
            this.layers = new List<Layer>(layers.Count);
            for (int i = 0; i < layers.Count; i++)
                this.layers.Add(new Layer(layers[i].Item1, layers[i].Item2, layers[i].Item3, layers[i].Item4, layers[i].Item5, layers[i].Item6, layers[i].Item7));
        }

        public Network(List<Layer> layers)
        {
            this.layers = new List<Layer>(layers);
        }

        public Network(Network parent, Random random)
        {
            this.layers = new List<Layer>(parent.layers.Count);

            for (int i = 0; i < parent.layers.Count; i++)
                this.layers.Add(new Layer(parent.layers[i]));

            this.Mutate(random);
        }

        public Network(Network parent1, Network parent2, Random random)
        {
            int layerCount = parent1.layers.Count;
            this.layers = new List<Layer>(layerCount);

            for (int i = 0; i < layerCount; i++)
                this.layers.Add(new Layer(parent1.layers[i], parent2.layers[i], random));

            this.Mutate(random);
        }

        public double[] Apply(double[] input, int inputWidth, int inputHeight, int inputChannels)
        {
            int currentWidth = inputWidth, currentHeight = inputHeight, currentChannels = inputChannels;
            double[] currentData = new double[inputWidth * inputHeight * inputChannels];
            input.CopyTo(currentData, 0);
            for (int i = 0; i < this.layers.Count; i++)
            {
                int nextWidth = (1 + currentWidth - this.layers[i].InputWidth) * this.layers[i].OutputWidth,
                    nextHeight = (1 + currentHeight - this.layers[i].InputHeight) * this.layers[i].OutputHeight;
                double[] nextData = new double[(1 + currentWidth - this.layers[i].InputWidth) * (1 + currentHeight - this.layers[i].InputHeight) * this.layers[i].OutputSize];
                for (int py = 0; py < nextHeight; py++)
                    for (int px = 0; px < nextWidth; px++)
                    {
                        double[] patch = new double[this.layers[i].InputSize];
                        for (int iy = 0; iy < this.layers[i].InputHeight; iy++)
                            Array.Copy(currentData, (py + iy) * this.layers[i].InputWidth * this.layers[i].InputChannels +px* this.layers[i].InputChannels,
                                       patch, iy * this.layers[i].InputWidth * this.layers[i].InputChannels, this.layers[i].InputWidth * this.layers[i].InputChannels);

                        double[] output = this.layers[i].Apply(patch);
                        for (int oy = 0; oy < this.layers[i].OutputHeight; oy++)
                            Array.Copy(output, oy * this.layers[i].OutputWidth * this.layers[i].OutputChannels,
                                       nextData, (py * this.layers[i].OutputHeight + oy) * this.layers[i].OutputWidth * this.layers[i].OutputChannels + px * this.layers[i].OutputWidth * this.layers[i].OutputChannels, this.layers[i].OutputWidth * this.layers[i].OutputChannels);

                    }
                currentData = nextData;
            }
            return currentData;
        }

        public void Mutate(Random random)
        {
            for (int i = 0; i < this.layers.Count; i++)
            {
                for (int oy = 0; oy < this.layers[i].OutputHeight; oy++)
                    for (int ox = 0; ox < this.layers[i].OutputWidth; ox++)
                        for (int oc = 0; oc < this.layers[i].OutputChannels; oc++)
                        {
                            this.layers[i][ox, oy, oc] += Program.ATanh(random.NextDouble() * 2 - 1) / 2;
                            for (int iy = 0; iy < this.layers[i].InputHeight; iy++)
                                for (int ix = 0; ix < this.layers[i].InputWidth; ix++)
                                    for (int ic = 0; ic < this.layers[i].InputChannels; ic++)
                                        this.layers[i][ix, iy, ic, ox, oy, oc] += Program.ATanh(random.NextDouble() * 2 - 1) / 2;
                        }
            }
        }
    }
}
