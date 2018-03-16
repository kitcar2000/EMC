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
                                                                                                                                                                                       oc];
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

        public Network(List<(int, int, int, int, int, int, Layer.ActivationFunction)> layers)
        {
            this.layers = new List<Layer>(layers.Count);
            for (int i = 0; i < layers.Count; i++)
                this.layers[i] = new Layer(layers[i].Item1, layers[i].Item2, layers[i].Item3, layers[i].Item4, layers[i].Item5, layers[i].Item6, layers[i].Item7);
            // 
        }

        public Network(List<Layer> layers)
        {
            this.layers = new List<Layer>(layers);
        }

        double[] Apply(double[] input, int inputWidth, int inputHeight, int inputChannels)
        {
            double[] previous = new double[inputWidth * inputHeight * inputChannels],
                     next = null;
            input.CopyTo(previous, 0);
            for (int i = 0; i < this.layers.Count; i++)
            {
                next = new double[(inputWidth - this.layers[i].InputWidth + 1) * (inputHeight - this.layers[i].InputHeight + 1) * (inputChannels - this.layers[i].InputChannels + 1) * this.layers[i].OutputSize];
                for (int py = 0; py < inputHeight - this.layers[i].InputHeight; py++)
                    for (int px = 0; px < inputWidth - this.layers[i].InputWidth; px++)
                    {
                        double[] patch = new double[this.layers[i].InputSize];
                        for (int iy = 0; iy < this.layers[i].InputHeight; iy++)
                            Array.Copy(previous, (py + iy) * inputWidth * inputChannels, 
                                       patch, iy * inputWidth * inputChannels, inputWidth * inputChannels);

                        double[] output = this.layers[i].Apply(patch);
                        for (int oy = 0; oy < this.layers[i].OutputHeight; oy++)
                            Array.Copy(output, oy * this.layers[i].OutputWidth * this.layers[i].OutputChannels, 
                                       next, (py * this.layers[i].OutputHeight + oy) * this.layers[i].OutputWidth * this.layers[i].OutputChannels + px * this.layers[i].OutputWidth * this.layers[i].OutputChannels, this.layers[i].OutputWidth * this.layers[i].OutputChannels);

                    }
                previous = next;
            }
            return previous;
        }
    }
}
