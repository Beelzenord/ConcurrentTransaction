using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TestConcurrentAPI
{
    public class UnitTest2
    {
        [Fact]
        public async Task PostPayment_ReturnsOk_With6UniquePayments()
        {

            var firstPayment = new Payment
            {
                ClientId = 1,
                DebtorAccount = "DebtorA",
                CreditorAccount = "CreditorB",
                InstructedAmount = "100.0",
                Currency = "USD"
            };

            var secondPayment = new Payment
            {
                ClientId = 2,
                DebtorAccount = "CreditorF",
                CreditorAccount = "CreditorC",
                InstructedAmount = "200.0",
                Currency = "USD"
            };

            var thirdPayment = new Payment
            {
                ClientId = 3,
                DebtorAccount = "DebtorD",
                CreditorAccount = "CreditorE",
                InstructedAmount = "300.0",
                Currency = "USD"
            };
            var fourthPayment = new Payment
            {
                ClientId = 4,
                DebtorAccount = "DebtorG",
                CreditorAccount = "CreditorH",
                InstructedAmount = "100.0",
                Currency = "USD"
            };

            var fifthPayment = new Payment
            {
                ClientId = 5,
                DebtorAccount = "CreditorI",
                CreditorAccount = "CreditorJ",
                InstructedAmount = "200.0",
                Currency = "USD"
            };

            var sixthPayment = new Payment
            {
                ClientId = 6,
                DebtorAccount = "DebtorK",
                CreditorAccount = "CreditorL",
                InstructedAmount = "300.0",
                Currency = "USD"
            };

            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            HttpRequestMessage CreateRequest(string clientId, Payment payment) =>
                new HttpRequestMessage(HttpMethod.Post, "/payments")
                {
                    Content = JsonContent.Create(payment),
                    Headers =
                    {
                { "ClientId", clientId }
                    }
                };

            var stopwatch1 = Stopwatch.StartNew();
            var task1 = Task.Run(() => client.SendAsync(CreateRequest("1", firstPayment)));
            var task2 = Task.Run(() => client.SendAsync(CreateRequest("2", secondPayment)));
            var task3 = Task.Run(() => client.SendAsync(CreateRequest("3", thirdPayment)));
            var task4 = Task.Run(() => client.SendAsync(CreateRequest("4", fourthPayment)));
            var task5 = Task.Run(() => client.SendAsync(CreateRequest("5", fifthPayment)));
            var task6 = Task.Run(() => client.SendAsync(CreateRequest("6", sixthPayment)));


            var responses = await Task.WhenAll(task1, task2, task3,task4,task5,task6);
            stopwatch1.Stop();
            Console.WriteLine($"Time elapsed after Function1: {stopwatch1.Elapsed.TotalSeconds} seconds");
            responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[1].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[2].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[3].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[4].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[5].StatusCode.Should().Be(HttpStatusCode.OK);


        }
        [Fact]
        public async Task PostPayment_ReturnsOk_WithOneFailOneRetrySucceed()
        {

            var firstPayment = new Payment
            {
                ClientId = 1,
                DebtorAccount = "DebtorA",
                CreditorAccount = "CreditorB",
                InstructedAmount = "100.0",
                Currency = "USD"
            };

            var secondPayment = new Payment
            {
                ClientId = 2,
                DebtorAccount = "CreditorF",
                CreditorAccount = "CreditorC",
                InstructedAmount = "200.0",
                Currency = "USD"
            };

            var thirdPayment = new Payment
            {
                ClientId = 3,
                DebtorAccount = "DebtorD",
                CreditorAccount = "CreditorE",
                InstructedAmount = "300.0",
                Currency = "USD"
            };
            var fourthPayment = new Payment
            {
                ClientId = 4,
                DebtorAccount = "DebtorG",
                CreditorAccount = "CreditorH",
                InstructedAmount = "100.0",
                Currency = "USD"
            };

            var fifthPayment = new Payment
            {
                ClientId = 5,
                DebtorAccount = "CreditorI",
                CreditorAccount = "CreditorJ",
                InstructedAmount = "200.0",
                Currency = "USD"
            };

            var sixthPayment = new Payment
            {
                ClientId = 6,
                DebtorAccount = "DebtorK",
                CreditorAccount = "CreditorL",
                InstructedAmount = "300.0",
                Currency = "USD"
            };

            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            HttpRequestMessage CreateRequest(string clientId, Payment payment) =>
                new HttpRequestMessage(HttpMethod.Post, "/payments")
                {
                    Content = JsonContent.Create(payment),
                    Headers =
                    {
                { "ClientId", clientId }
                    }
                };

            var stopwatch1 = Stopwatch.StartNew();
            var task1 = Task.Run(() => client.SendAsync(CreateRequest("1", firstPayment)));
            var task2 = Task.Run(() => client.SendAsync(CreateRequest("2", secondPayment)));
            await Task.Delay(1000);
            var task3 = Task.Run(() => client.SendAsync(CreateRequest("1", firstPayment)));
            var task4 = Task.Run(() => client.SendAsync(CreateRequest("4", fourthPayment)));
            var task5 = Task.Run(() => client.SendAsync(CreateRequest("5", fifthPayment)));
            await Task.Delay(2000);
            var task6 = Task.Run(() => client.SendAsync(CreateRequest("1", firstPayment)));


            var responses = await Task.WhenAll(task1, task2, task3, task4, task5, task6);
            stopwatch1.Stop();
            Console.WriteLine($"Time elapsed after Function1: {stopwatch1.Elapsed.TotalSeconds} seconds");
            responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[1].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[2].StatusCode.Should().Be(HttpStatusCode.Conflict);
            responses[3].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[4].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[5].StatusCode.Should().Be(HttpStatusCode.OK);


        }

        [Fact]
        public async Task PostPayment_ReturnsOk_WithAHundredPayments()
        {

            // Arrange
            var payments = Enumerable.Range(1, 100).Select(i => new Payment
            {
                ClientId = i,
                DebtorAccount = $"Debtor{i}",
                CreditorAccount = $"Creditor{i}",
                InstructedAmount = (100 + i).ToString(),
                Currency = "USD"
            }).ToList();


         
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            HttpRequestMessage CreateRequest(Payment payment) =>
                new HttpRequestMessage(HttpMethod.Post, "/payments")
                {
                    Content = JsonContent.Create(payment),
                    Headers = { { "ClientId", payment.ClientId.ToString() } }
                };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = payments.Select(payment => client.SendAsync(CreateRequest(payment))).ToArray();
            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Console.WriteLine($"Time elapsed for 100 unique tasks: {stopwatch.Elapsed.TotalSeconds} seconds");

            // Verify all responses
            for (int i = 0; i < responses.Length; i++)
            {
                responses[i].StatusCode.Should().Be(HttpStatusCode.OK, $"Task {i + 1} should succeed.");
            }


        }

        [Fact]
        public async Task ProcessHundredPaymentsWithConflicts_ShouldCountConflicts()
        {
            // Arrange
            var payments = Enumerable.Range(1, 100).Select(i => new Payment
            {
                ClientId = i,
                DebtorAccount = $"Debtor{i}",
                CreditorAccount = $"Creditor{i}",
                InstructedAmount = (100 + i).ToString(),
                Currency = "USD"
            }).ToList();

            // Add 3 conflicting payment requests
            payments.Add(new Payment
            {
                ClientId = 1, // Duplicate ClientId
                DebtorAccount = "Debtor1",
                CreditorAccount = "Creditor1",
                InstructedAmount = "150.0",
                Currency = "USD"
            });

            payments.Add(new Payment
            {
                ClientId = 2, // Duplicate ClientId
                DebtorAccount = "Debtor2",
                CreditorAccount = "Creditor2",
                InstructedAmount = "250.0",
                Currency = "USD"
            });

            payments.Add(new Payment
            {
                ClientId = 3, // Duplicate ClientId
                DebtorAccount = "Debtor3",
                CreditorAccount = "Creditor3",
                InstructedAmount = "350.0",
                Currency = "USD"
            });

            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            HttpRequestMessage CreateRequest(Payment payment) =>
                new HttpRequestMessage(HttpMethod.Post, "/payments")
                {
                    Content = JsonContent.Create(payment),
                    Headers = { { "ClientId", payment.ClientId.ToString() } }
                };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = payments.Select(payment => client.SendAsync(CreateRequest(payment))).ToArray();
            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Count conflicts
            var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);

            // Assert
            Console.WriteLine($"Time elapsed for tasks with conflicts: {stopwatch.Elapsed.TotalSeconds} seconds");
            Console.WriteLine($"Success Count: {successCount}");
            Console.WriteLine($"Conflict Count: {conflictCount}");

            // Verify expected counts
            successCount.Should().Be(100, "The first 100 unique payments should succeed.");
            conflictCount.Should().Be(3, "The 3 conflicting payments should result in conflicts.");
        }

        [Fact]
        public async Task PostPayment_ReturnsOk_WithAThousandPayments()
        {

            // Arrange
            var payments = Enumerable.Range(1, 1000).Select(i => new Payment
            {
                ClientId = i,
                DebtorAccount = $"Debtor{i}",
                CreditorAccount = $"Creditor{i}",
                InstructedAmount = (100 + i).ToString(),
                Currency = "USD"
            }).ToList();



            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            HttpRequestMessage CreateRequest(Payment payment) =>
                new HttpRequestMessage(HttpMethod.Post, "/payments")
                {
                    Content = JsonContent.Create(payment),
                    Headers = { { "ClientId", payment.ClientId.ToString() } }
                };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = payments.Select(payment => client.SendAsync(CreateRequest(payment))).ToArray();
            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Console.WriteLine($"Time elapsed for 100 unique tasks: {stopwatch.Elapsed.TotalSeconds} seconds");

            // Verify all responses
            for (int i = 0; i < responses.Length; i++)
            {
                responses[i].StatusCode.Should().Be(HttpStatusCode.OK, $"Task {i + 1} should succeed.");
            }


        }
    }
}
