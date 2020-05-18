using System;
using System.Collections;
using UnityEngine;
using Unity.Barracuda;

namespace AIinGames
{
    public class BarracudaWorker : IDisposable
    {
        private IWorker worker;
        private Model model;
        private float[] results;
        private PrecompiledComputeOps ops;

        public BarracudaWorker(NNModel nnModel, WorkerFactory.Type type)
        {
            bool verbose = false;
            model = ModelLoader.Load(nnModel, verbose);
            worker = WorkerFactory.CreateWorker(type, model, verbose);

            var kernels = ComputeShaderSingleton.Instance.kernels;
            ops = new PrecompiledComputeOps(kernels, kernels[0]);
        }

        public void Dispose()
        {
            worker?.Dispose();
            model = null;
            ops = null;
        }

        public IEnumerator ExecuteAsync(Texture2D inputTex)
        {
            int width = inputTex.width;
            int height = inputTex.height;

            Tensor input = new Tensor(1, height, width, 1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // have to invert the y axis
                    input[0, 27 - y, x, 0] = inputTex.GetPixel(x, y).r;
                }
            }
            yield return worker.ExecuteAsync(input);

            Tensor output = worker.PeekOutput();
            //Tensor outSoftmax = ops.Softmax(output);
            results = output.data.Download(output.shape);

            input.Dispose();
            output.Dispose();
        }

        public IEnumerator ExecuteAsyncTexture(Texture2D inputTex)
        {
            Tensor input = new Tensor(inputTex, 1);
            yield return worker.ExecuteAsync(input);

            Tensor output = worker.PeekOutput();
            results = output.data.Download(output.shape);

            input.Dispose();
            output.Dispose();
        }

        public float[] GetResult()
        {
            return results;
        }

        public TensorShape GetInputShape(int index)
        {
            return new TensorShape(model.inputs[index].shape);
        }
    }
}
