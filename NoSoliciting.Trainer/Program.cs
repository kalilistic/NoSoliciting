using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ConsoleTables;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using NoSoliciting.Internal.Interface;

namespace NoSoliciting.Trainer {
    internal static class Program {
        private static readonly string[] StopWords = {
            "discord",
            "lgbt",
            "lgbtq",
            "lgbtqia",
            "http",
            "https",
            "18",
            "come",
            "join",
            "blu",
        };

        private enum Mode {
            Test,
            CreateModel,
            Interactive,
            InteractiveFull,
        }

        private static void Main(string[] args) {
            var mode = args[0] switch {
                "test" => Mode.Test,
                "create-model" => Mode.CreateModel,
                "interactive" => Mode.Interactive,
                "interactive-full" => Mode.InteractiveFull,
                _ => throw new ArgumentException("invalid argument"),
            };
            var path = "../../../data.csv";

            if (args.Length > 1) {
                path = args[1];
            }

            var parentDir = Directory.GetParent(path);

            var ctx = new MLContext(1);

            List<Data> records;

            using (var fileStream = new FileStream(path, FileMode.Open)) {
                using var stream = new StreamReader(fileStream);
                using var csv = new CsvReader(stream, new CsvConfiguration(CultureInfo.InvariantCulture) {
                    HeaderValidated = null,
                });
                records = csv
                    .GetRecords<Data>()
                    .Select(rec => {
                        rec.Message = rec.Message
                            .Replace("\r\n", " ")
                            .Replace("\r", " ")
                            .Replace("\n", " ");
                        return rec;
                    })
                    .OrderBy(rec => rec.Category)
                    .ThenBy(rec => rec.Channel)
                    .ThenBy(rec => rec.Message)
                    .ToList();
            }

            using (var fileStream = new FileStream(path, FileMode.Create)) {
                using var stream = new StreamWriter(fileStream);
                using var csv = new CsvWriter(stream, new CsvConfiguration(CultureInfo.InvariantCulture) {
                    NewLine = "\n",
                });
                csv.WriteRecords(records);
            }

            var classes = new Dictionary<string, uint>();

            foreach (var record in records) {
                // keep track of how many message of each category we have
                if (!classes.ContainsKey(record.Category!)) {
                    classes[record.Category] = 0;
                }

                classes[record.Category] += 1;
            }

            // calculate class weights
            var weights = new Dictionary<string, float>();
            foreach (var (category, count) in classes) {
                var nSamples = (float) records.Count;
                var nClasses = (float) classes.Count;
                var nSamplesJ = (float) count;

                var w = nSamples / (nClasses * nSamplesJ);

                weights[category] = w;
            }

            var df = ctx.Data.LoadFromEnumerable(records);

            var ttd = ctx.Data.TrainTestSplit(df, 0.2, seed: 1);

            var compute = new Data.ComputeContext(weights);
            var normalise = new Data.Normalise();

            ctx.ComponentCatalog.RegisterAssembly(typeof(Data).Assembly);

            var pipeline = ctx.Transforms.Conversion.MapValueToKey("Label", nameof(Data.Category))
                .Append(ctx.Transforms.CustomMapping(compute.GetMapping(), "Compute"))
                .Append(ctx.Transforms.CustomMapping(normalise.GetMapping(), "Normalise"))
                .Append(ctx.Transforms.Text.NormalizeText("MsgNormal", nameof(Data.Normalise.Normalised.NormalisedMessage), keepPunctuations: false))
                .Append(ctx.Transforms.Text.TokenizeIntoWords("MsgTokens", "MsgNormal"))
                .Append(ctx.Transforms.Text.RemoveDefaultStopWords("MsgNoDefStop", "MsgTokens"))
                .Append(ctx.Transforms.Text.RemoveStopWords("MsgNoStop", "MsgNoDefStop", StopWords))
                .Append(ctx.Transforms.Conversion.MapValueToKey("MsgKey", "MsgNoStop"))
                .Append(ctx.Transforms.Text.ProduceNgrams("MsgNgrams", "MsgKey", weighting: NgramExtractingEstimator.WeightingCriteria.Tf))
                .Append(ctx.Transforms.NormalizeLpNorm("FeaturisedMessage", "MsgNgrams"))
                .Append(ctx.Transforms.Conversion.ConvertType("CPartyFinder", "PartyFinder"))
                .Append(ctx.Transforms.Conversion.ConvertType("CShout", "Shout"))
                .Append(ctx.Transforms.Conversion.ConvertType("CTrade", "ContainsTradeWords"))
                .Append(ctx.Transforms.Conversion.ConvertType("CSketch", "ContainsSketchUrl"))
                .Append(ctx.Transforms.Conversion.ConvertType("HasWard", "ContainsWard"))
                .Append(ctx.Transforms.Conversion.ConvertType("HasPlot", "ContainsPlot"))
                .Append(ctx.Transforms.Conversion.ConvertType("HasNumbers", "ContainsHousingNumbers"))
                .Append(ctx.Transforms.Concatenate("Features", "FeaturisedMessage", "CPartyFinder", "CShout", "CTrade", "HasWard", "HasPlot", "HasNumbers", "CSketch"))
                .Append(ctx.MulticlassClassification.Trainers.SdcaMaximumEntropy(exampleWeightColumnName: "Weight"))
                .Append(ctx.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var train = mode switch {
                Mode.Test => ttd.TrainSet,
                Mode.CreateModel => df,
                Mode.Interactive => ttd.TrainSet,
                Mode.InteractiveFull => df,
            };

            var model = pipeline.Fit(train);

            if (mode == Mode.CreateModel) {
                var savePath = Path.Join(parentDir.FullName, "model.zip");
                ctx.Model.Save(model, train.Schema, savePath);
            }

            var testPredictions = model.Transform(ttd.TestSet);
            var eval = ctx.MulticlassClassification.Evaluate(testPredictions);

            var predEngine = ctx.Model.CreatePredictionEngine<Data, Prediction>(model);

            var slotNames = new VBuffer<ReadOnlyMemory<char>>();
            predEngine.OutputSchema["Score"].GetSlotNames(ref slotNames);
            var names = slotNames.DenseValues()
                .Select(column => column.ToString())
                .ToList();

            var cols = new string[1 + names.Count];
            cols[0] = "";
            for (var j = 0; j < names.Count; j++) {
                cols[j + 1] = names[j];
            }

            var table = new ConsoleTable(cols);

            for (var i = 0; i < names.Count; i++) {
                var name = names[i];
                var confuse = eval.ConfusionMatrix.Counts[i];

                var row = new object[1 + confuse.Count];
                row[0] = name;
                for (var j = 0; j < confuse.Count; j++) {
                    row[j + 1] = confuse[j];
                }

                table.AddRow(row);
            }

            Console.WriteLine(table.ToString());

            Console.WriteLine($"Log loss : {eval.LogLoss * 100}");
            Console.WriteLine($"Macro acc: {eval.MacroAccuracy * 100}");
            Console.WriteLine($"Micro acc: {eval.MicroAccuracy * 100}");

            switch (mode) {
                case Mode.Test:
                case Mode.CreateModel:
                    return;
            }

            while (true) {
                var msg = Console.ReadLine()!.Trim();

                var parts = msg.Split(' ', 2);

                ushort.TryParse(parts[0], out var channel);

                var input = new Data(channel, parts[1]);
                var pred = predEngine.Predict(input);

                Console.WriteLine(pred.Category);
                for (var i = 0; i < names.Count; i++) {
                    Console.WriteLine($"    {names[i]}: {pred.Probabilities[i] * 100}");
                }
            }
        }
    }
}
