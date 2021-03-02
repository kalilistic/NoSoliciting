using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JKang.IpcServiceFramework.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ML;
using NoSoliciting.Interface;
using NoSoliciting.Internal.Interface;

namespace NoSoliciting.MessageClassifier {
    internal static class Program {
        private static void Main(string[] args) {
            if (!int.TryParse(args[0], out var gamePid)) {
                Console.WriteLine("No game PID provided.");
                return;
            }

            var gameName = args[1];
            var pipeId = args[2];

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services => {
                    services.AddSingleton<IClassifier, ClassifierService>();
                })
                .ConfigureIpcHost(builder => {
                    builder.AddNamedPipeEndpoint<IClassifier>(options => {
                        options.PipeName = $"NoSoliciting.MessageClassifier-{pipeId}";
                        options.Serializer = new BetterIpcSerialiser();
                    });
                })
                .Build();

            Task.Run(async () => {
                while (true) {
                    Process process;
                    try {
                        process = Process.GetProcessById(gamePid);
                    } catch (Exception) {
                        await host.StopAsync();
                        return;
                    }

                    if (process.ProcessName != gameName) {
                        await host.StopAsync();
                        return;
                    }

                    await Task.Delay(5_000);
                }
            });

            host.Run();
        }
    }

    internal class ClassifierService : IClassifier, IDisposable {
        private MLContext Context { get; set; } = null!;
        private ITransformer Model { get; set; } = null!;
        private DataViewSchema Schema { get; set; } = null!;
        private PredictionEngine<Data, Prediction>? PredictionEngine { get; set; }

        public void Initialise(byte[] data) {
            if (this.PredictionEngine != null) {
                this.PredictionEngine.Dispose();
                this.PredictionEngine = null;
            }

            this.Context = new MLContext();
            this.Context.ComponentCatalog.RegisterAssembly(typeof(Data).Assembly);
            using var stream = new MemoryStream(data);
            var model = this.Context.Model.Load(stream, out var schema);
            this.Model = model;
            this.Schema = schema;
            this.PredictionEngine = this.Context.Model.CreatePredictionEngine<Data, Prediction>(this.Model, this.Schema);
        }

        public string Classify(ushort channel, string message) {
            return this.PredictionEngine?.Predict(new Data(channel, message))?.Category ?? "UNKNOWN";
        }

        public void Dispose() {
            this.PredictionEngine?.Dispose();
        }
    }
}
