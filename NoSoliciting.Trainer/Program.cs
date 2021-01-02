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
using NoSoliciting.Interface;

namespace NoSoliciting.Trainer {
    internal static class Program {
        private static void Main(string[] args) {
            var full = args[0] == "create";

            var ctx = new MLContext(1);

            List<Data> records;

            using (var fileStream = new FileStream("../../../data.csv", FileMode.Open)) {
                using var stream = new StreamReader(fileStream);
                using var csv = new CsvReader(stream, new CsvConfiguration(CultureInfo.InvariantCulture) {
                    HeaderValidated = null,
                });
                records = csv
                    .GetRecords<Data>()
                    .OrderBy(rec => rec.Category)
                    .ThenBy(rec => rec.Channel)
                    .ThenBy(rec => rec.Message)
                    .ToList();
            }

            using (var fileStream = new FileStream("../../../data.csv", FileMode.Create)) {
                using var stream = new StreamWriter(fileStream);
                using var csv = new CsvWriter(stream, new CsvConfiguration(CultureInfo.InvariantCulture));
                csv.WriteRecords(records);
            }

            var classes = new Dictionary<string, uint>();

            foreach (var record in records) {
                // normalise the message
                record.Message = Util.Normalise(record.Message);

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

            ctx.ComponentCatalog.RegisterAssembly(typeof(Data).Assembly);

            var pipeline = ctx.Transforms.Conversion.MapValueToKey("Label", nameof(Data.Category))
                .Append(ctx.Transforms.CustomMapping(compute.GetMapping(), "Compute"))
                .Append(ctx.Transforms.Text.NormalizeText("MsgNormal", nameof(Data.Message), keepPunctuations: false))
                .Append(ctx.Transforms.Text.TokenizeIntoWords("MsgTokens", "MsgNormal"))
                // .Append(ctx.Transforms.Text.RemoveStopWords("MsgNoStop", "MsgTokens",
                //     "the",
                //     "a",
                //     "of",
                //     "in",
                //     "for",
                //     "from",
                //     "and",
                //     "discord"
                // ))
                .Append(ctx.Transforms.Text.RemoveDefaultStopWords("MsgNoDefStop", "MsgTokens"))
                .Append(ctx.Transforms.Text.RemoveStopWords("MsgNoStop", "MsgNoDefStop",
                    "discord"
                ))
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

            var train = full ? df : ttd.TrainSet;

            var model = pipeline.Fit(train);

            if (full) {
                ctx.Model.Save(model, train.Schema, @"../../../model.zip");
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

            if (full) {
                return;
            }

            while (true) {
                var msg = Console.ReadLine()!.Trim();

                var parts = msg.Split(' ', 2);

                ushort.TryParse(parts[0], out var channel);

                var input = new Data {
                    Channel = channel,
                    // PartyFinder = channel == 0,
                    Message = parts[1],
                };

                var pred = predEngine.Predict(input);

                Console.WriteLine(pred.Category);
                for (var i = 0; i < names.Count; i++) {
                    Console.WriteLine($"    {names[i]}: {pred.Probabilities[i] * 100}");
                }
            }
        }
    }

    internal static class Ext {
        public static bool ContainsIgnoreCase(this string haystack, string needle) {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
        }
    }
}
