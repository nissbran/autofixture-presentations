﻿namespace Demo.Bank.EventPublisher
{
    using System;
    using System.Threading.Tasks;
    using Domain.Models.Account;
    using EventStore.Lib.Common;
    using EventStore.Lib.Common.Configurations;
    using EventStore.Lib.Write.Persistance;
    using Logger;

    public class Program
    {
        public static void Main(string[] args)
        {
            var eventStoreConnection = EventStoreConnectionFactory.Create(
                new EventStore3NodeClusterConfiguration(), 
                new EventStoreLogger(),
                "admin", "changeit");

            const string accountNumberPrefix = "1206";

            eventStoreConnection.ConnectAsync().Wait();

            var eventStore = new EventStoreDomainRepository(eventStoreConnection);

            var tasks = new Task[50];

            for (int i = 0; i < 50; i++)
            {
                try
                {
                    var account = eventStore.GetById<Account>($"{accountNumberPrefix}-{i}").Result;
                }
                catch (Exception)
                {
                    var account = new Account($"{accountNumberPrefix}-{i}");
                    eventStore.Save(account).Wait();
                }

            }
            for (int i = 0; i < 50; i++)
            {
                var accountNumber = i;

                tasks[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < 50; j++)
                    {
                        var account = await eventStore.GetById<Account>($"{accountNumberPrefix}-{accountNumber}");
                        //await Task.Delay(500);
                        account.AddBankTransferTransaction(5);
                        account.AddBankTransferTransaction(5);
                        account.AddBankTransferTransaction(5);
                        account.AddBankTransferTransaction(5);
                        await eventStore.Save(account);
                    }
                });
            }

            Task.WaitAll(tasks);
            
            Console.WriteLine("All done");

            Console.ReadLine();
        }
    }
}
