using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace ChinhDo.KedaPoc.Consumer
{       
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Random _rnd = new Random();

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = await LoadConfig("kafka.properties", String.Empty);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker V109 running at: {time}", DateTimeOffset.Now);

                try
                {
                    Consume("kedapoc", config);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private void Consume(string topic, ClientConfig config)
        {            
            // https://confluent.cloud/environments/env-2v1v2/clusters/lkc-7ypg0o/clients/setup/csharp
            var consumerConfig = new ConsumerConfig(config);
            consumerConfig.GroupId = "kedapoc";
            consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
            consumerConfig.EnableAutoCommit = false;
            consumerConfig.EnableAutoOffsetStore = false;

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
            {
                consumer.Subscribe(topic);
                var totalCount = 0;
                try
                {
                    while (true)
                    {
                        var cr = consumer.Consume(cts.Token);
                        Stopwatch w = new Stopwatch();
                        w.Start();
                        Thread.Sleep(4000 + _rnd.Next(2000));
                        totalCount += JObject.Parse(cr.Message.Value).Value<int>("count");
                        w.Stop();
                        consumer.Commit(cr);
                        consumer.StoreOffset(cr);                        

                        // Console.WriteLine($"Consumed record with key {cr.Message.Key} and value {cr.Message.Value}, and updated total count to {totalCount}");
                        Console.WriteLine($"Consumed record with key {cr.Message.Key}. Elapsed: {w.ElapsedMilliseconds}.");

                    }
                }
                catch (OperationCanceledException)
                {
                    // Ctrl-C was pressed.
                }
                finally
                {
                    consumer.Close();
                }
            }
        }

        async Task<ClientConfig> LoadConfig(string configPath, string certDir)
        {
            try
            {
                var cloudConfig = (await File.ReadAllLinesAsync(configPath))
                    .Where(line => !line.StartsWith("#"))
                    .ToDictionary(
                        line => line.Substring(0, line.IndexOf('=')),
                        line => line.Substring(line.IndexOf('=') + 1));

                var clientConfig = new ClientConfig(cloudConfig);

                if (certDir != null)
                {
                    clientConfig.SslCaLocation = certDir;
                }

                return clientConfig;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured reading the config file from '{configPath}': {e.ToString()}");
                System.Environment.Exit(1);
                return null; // avoid not-all-paths-return-value compiler error.
            }
        }
    }
}