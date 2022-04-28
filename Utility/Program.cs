// See https://aka.ms/new-console-template for more information
using ChinhDo.KedaPoc.Producer;
using System.CommandLine;

Console.WriteLine("Starting producer.");

RootCommand rootCommand = new RootCommand("KEDA POC Utility.");
rootCommand.TreatUnmatchedTokensAsErrors = true;

var msgsOption = new Option<int?>(name: "--msgs", description: "Number of Kafka messages to send.");
rootCommand.AddOption(msgsOption);

rootCommand.SetHandler(async (int? msgs) => { 
    Console.WriteLine($"Sending {msgs} messages.");

    var util = new Utility();
    await util.ProduceMessages(msgs);
}, msgsOption);

return await rootCommand.InvokeAsync(args);