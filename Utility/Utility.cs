using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChinhDo.KedaPoc.Producer
{
    internal class Utility
    {
        public async Task ProduceMessages(int? numMessages)
        {
            Console.WriteLine("Starting.");

            var config = await LoadConfig("kafka.properties", String.Empty);
            var topic = "kedapoc";
            int msgId = 1;
            string idFileName = "~id.txt";
            if (File.Exists(idFileName)) {
                msgId = int.Parse(File.ReadAllText(idFileName));
            }

            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                int numProduced = 0;
                for (int i = 0; i < numMessages; ++i)
                {
                    var key = msgId.ToString();
                    var val = JObject.FromObject(new { count = i }).ToString(Formatting.None);

                    Console.WriteLine($"Producing record: {key} {val}");

                    producer.Produce(topic, new Message<string, string> { Key = key, Value = val },
                        (deliveryReport) =>
                        {
                            if (deliveryReport.Error.Code != ErrorCode.NoError)
                            {
                                Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");
                            }
                            else
                            {
                                Console.WriteLine($"Produced message to: {deliveryReport.TopicPartitionOffset}");
                                numProduced += 1;
                            }
                        });

                    msgId++;
                }

                producer.Flush(TimeSpan.FromSeconds(10));
                File.WriteAllText(idFileName, msgId.ToString());

                Console.WriteLine($"{numProduced} messages were produced to topic {topic}");
            }
        }

        //public async Task Start(int numMessages)
        //{
        //    Console.WriteLine("Starting.");
        //    var config = await LoadConfig("kafka.properties", String.Empty);
        //    var topic = "kedapoc";

        //    using (var producer = new ProducerBuilder<string, string>(config).Build())
        //    {
        //        int numProduced = 0;
        //        for (int i = 0; i < numMessages; ++i)
        //        {
        //            var key = i.ToString();
        //            var val = JObject.FromObject(new { count = i }).ToString(Formatting.None);

        //            Console.WriteLine($"Producing record: {key} {val}");

        //            producer.Produce(topic, new Message<string, string> { Key = key, Value = val },
        //                (deliveryReport) =>
        //                {
        //                    if (deliveryReport.Error.Code != ErrorCode.NoError)
        //                    {
        //                        Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");
        //                    }
        //                    else
        //                    {
        //                        Console.WriteLine($"Produced message to: {deliveryReport.TopicPartitionOffset}");
        //                        numProduced += 1;
        //                    }
        //                });
        //        }

        //        producer.Flush(TimeSpan.FromSeconds(10));

        //        Console.WriteLine($"{numProduced} messages were produced to topic {topic}");
        //    }
        //}

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
