using ConcurrentTransactions.API.Channel;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net.Http.Json;
using System.Net;
using Moq;
using ConcurrentTransactions.API.Model;
using FluentAssertions;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Diagnostics;

namespace TestConcurrentAPI
{
    public class UnitTest1 : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
       
        public UnitTest1(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }
      
            [Fact]
            public async Task MakeOneTransaction()
           {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
                DebtorAccount = "IBAN123",
                CreditorAccount = "IBAN456",
                InstructedAmount = "100.0",
                Currency = "USD"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(payment)
            };

            request.Headers.Add("ClientId", "12345");

              
            var response = await client.SendAsync(request);

             
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<Payment>();
            result.Should().NotBeNull();
            

        }
        [Fact]
        public async Task PostPayment_ReturnsOk_WithAllUniquePayments()
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


            var responses = await Task.WhenAll(task1, task2, task3);
            stopwatch1.Stop();
            Console.WriteLine($"Time elapsed after Function1: {stopwatch1.Elapsed.TotalSeconds} seconds");
            responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[1].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[2].StatusCode.Should().Be(HttpStatusCode.OK);


        }
        [Fact]
        public async Task PostPayment_ReturnsOk_WhenTransactionSucceeds()
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
                DebtorAccount = "CreditorB",
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


            var task1 = Task.Run(() => client.SendAsync(CreateRequest("1", firstPayment)));
            await Task.Delay(2200); // Delay the second payment
            var task2 = Task.Run(() => client.SendAsync(CreateRequest("2", secondPayment)));
            var task3 = Task.Run(() => client.SendAsync(CreateRequest("3", thirdPayment)));


            var responses = await Task.WhenAll(task1, task2, task3);

            responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[1].StatusCode.Should().Be(HttpStatusCode.OK); 
            responses[2].StatusCode.Should().Be(HttpStatusCode.OK);


        }
        [Fact]
        public async Task PostPayment_ReturnsOk_WhenTransactionSucceedsOneWillConflict()
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
                DebtorAccount = "CreditorB",  
                CreditorAccount = "CreditorC",
                InstructedAmount = "200.0",
                Currency = "USD"
            };

            var thirdPayment = new Payment
            {
                ClientId = 3,
                DebtorAccount = "DebtorD",
                CreditorAccount = "CreditorE",
                InstructedAmount ="300.0",
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

           
            var task1 = Task.Run(() => client.SendAsync(CreateRequest("1", firstPayment)));
            await Task.Delay(1000); // Delay the second payment
            var task2 = Task.Run(() => client.SendAsync(CreateRequest("2", secondPayment)));
            var task3 = Task.Run(() => client.SendAsync(CreateRequest("3", thirdPayment)));


            var responses = await Task.WhenAll(task1, task2, task3);

            responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[1].StatusCode.Should().Be(HttpStatusCode.Conflict); // this task should conflict 
            responses[2].StatusCode.Should().Be(HttpStatusCode.OK);   
        }

        [Fact]
        public async Task PostPayment_MakePayment_GetListNoConflict()
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
                DebtorAccount = "CreditorB",
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


            var task1 = Task.Run(() => client.SendAsync(CreateRequest("1", firstPayment)));
            await Task.Delay(2100); // Delay the second payment
            var task2 = Task.Run(() => client.SendAsync(CreateRequest("2", secondPayment)));
            var task3 = Task.Run(() => client.SendAsync(CreateRequest("3", thirdPayment)));


            var responses = await Task.WhenAll(task1, task2, task3);

            responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[1].StatusCode.Should().Be(HttpStatusCode.OK);  // Shouldn't conflict because of the delay
            responses[2].StatusCode.Should().Be(HttpStatusCode.OK);

            await Task.Delay(2500);
              var iban = "CreditorB";
            var getResponse = await client.GetAsync($"/accounts/{iban}/transactions");

            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var getResult = await getResponse.Content.ReadFromJsonAsync<SnapshotResponse>();
            
            getResult.Should().NotBeNull();
            getResult!.Transactions.Should().HaveCount(2); // CreditorB appears in two transactions
            getResult.Transactions.Should().Contain(t => t.DebtorAccount == "CreditorB" && t.CreditorAccount == "CreditorC");
            getResult.Transactions.Should().Contain(t => t.DebtorAccount == "DebtorA" && t.CreditorAccount == "CreditorB");

            getResult!.Transactions.Should().HaveCount(2); // CreditorB appears in two transactions
            getResult.Transactions.Should().Contain(t => t.DebtorAccount == "DebtorA" && t.CreditorAccount == "CreditorB");
            getResult.Transactions.Should().Contain(t => t.DebtorAccount == "CreditorB" && t.CreditorAccount == "CreditorC");

          
        }

        [Fact]
        public async Task PostPayment_MakePayment_GetListOneConflict()
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
                DebtorAccount = "CreditorB",
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

            var iban = "CreditorE";
            var task1 = Task.Run(() => client.SendAsync(CreateRequest("1", firstPayment)));
            await Task.Delay(3000); 
            var task2 = Task.Run(() => client.SendAsync(CreateRequest("2", secondPayment)));
            var task3 = Task.Run(() => client.SendAsync(CreateRequest("3", thirdPayment)));
            var task4 = Task.Run(() => client.GetAsync($"/accounts/{iban}/transactions"));

            var responses = await Task.WhenAll(task1, task2, task3,task4);

            responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[1].StatusCode.Should().Be(HttpStatusCode.OK); 
            responses[2].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[3].StatusCode.Should().Be(HttpStatusCode.NoContent);
             
            
            var getResponse = await client.GetAsync($"/accounts/{iban}/transactions");

        }


    }
}
