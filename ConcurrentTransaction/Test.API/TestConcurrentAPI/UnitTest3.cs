using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace TestConcurrentAPI
{

    public class UnitTest3
    {
        [Fact]
        public async Task PostPaymentWithMissingAccountFieldValue()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
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


            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        }

        [Fact]
        public async Task PostPaymentWithMissingAccountMissingCurrencyValue()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
                CreditorAccount = "IBAN456",
                DebtorAccount = "12345",
                InstructedAmount = "100.0",
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(payment)
            };

            request.Headers.Add("ClientId", "12345");


            var response = await client.SendAsync(request);


            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        }
        [Fact]
        public async Task PostPaymentWithMissingAccountMissingDebtorValue()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
                CreditorAccount = "IBAN456",
                InstructedAmount = "100.0",
                Currency = "SEK"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(payment)
            };

            request.Headers.Add("ClientId", "12345");


            var response = await client.SendAsync(request);


            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        }
        [Fact]
        public async Task PostPaymentWithInvalidClientId()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
                DebtorAccount = "12345",
                CreditorAccount = "IBAN456",
                InstructedAmount = "100.0",
                Currency = "SEK"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(payment)
            };

            request.Headers.Add("ClientId", "ONE!");


            var response = await client.SendAsync(request);


            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        }

        [Fact]
        public async Task PostPaymentWithDebtorAccountLargerThan32Characters()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
                DebtorAccount = "12345",
                CreditorAccount = "IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456IBAN456",
                InstructedAmount = "100.0",
                Currency = "SEK"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(payment)
            };

            request.Headers.Add("ClientId", "1");


            var response = await client.SendAsync(request);


            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        }
        [Fact]
        public async Task PostWithInvalidCurrency()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
                DebtorAccount = "12345",
                CreditorAccount = "IBAN4",
                InstructedAmount = "100.0",
                Currency = "S1K"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(payment)
            };

            request.Headers.Add("ClientId", "1");


            var response = await client.SendAsync(request);


            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        }
        [Fact]
        public async Task PostWithLowercaseButValidCurrency()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
                DebtorAccount = "12345",
                CreditorAccount = "IBAN4",
                InstructedAmount = "100.0",
                Currency = "usd"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(payment)
            };

            request.Headers.Add("ClientId", "1");


            var response = await client.SendAsync(request);


            response.StatusCode.Should().Be(HttpStatusCode.OK);

        }
        [Fact]
        public async Task PostWithInvalidInstructedAmount()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var payment = new Payment
            {
                DebtorAccount = "12345",
                CreditorAccount = "IBAN4",
                InstructedAmount = "One",
                Currency = "usd"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(payment)
            };

            request.Headers.Add("ClientId", "1");


            var response = await client.SendAsync(request);


            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        }
    }
}
