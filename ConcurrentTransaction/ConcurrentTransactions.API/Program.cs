using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using System.Web.Mvc;
using ConcurrentTransactions.API.Channel;
using ConcurrentTransactions.API.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(); 
builder.Logging.AddDebug();    


builder.Services.AddOpenApi();
builder.Services.AddSingleton<TransactionTracker>();

builder.Services.AddSingleton<TransactionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Test Transactions");
            options.WithTheme(ScalarTheme.DeepSpace);
            options.WithSidebar(true);
         });

    }
}


app.MapGet("/", () => "Hello World!");

app.MapGet("/accounts/{iban}/transactions", async (string iban, TransactionHandler transactionHandler, CancellationToken cancellationToken) =>
{
    try
    {
        var (snapshotTime, transactions) = await transactionHandler.GetSnapshotOfConfirmedTransactions(iban);

        return Results.Ok(new
        {
            SnapshotTime = snapshotTime,
            Transactions = transactions
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.NoContent();
    }
});


app.MapPost("/alternate", async (Payment paymentRequest, HttpRequest request, [FromServices] TransactionHandler transactionHandler) =>
{
    if (!request.Headers.TryGetValue("ClientId", out var ClientId))
    {
        return Results.BadRequest("Missing required header: ClientHeader");
    }
    if (!int.TryParse(ClientId, out var ClientIdAsInt))
    {
        return Results.BadRequest("X-Custom-Header must be a valid integer.");
    }
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(paymentRequest);

    if (!Validator.TryValidateObject(paymentRequest, validationContext, validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(v => new
        {
            Field = v.MemberNames.FirstOrDefault() ?? "General",
            Error = v.ErrorMessage
        }));
    }
    try
    {
        if (string.Equals(paymentRequest.CreditorAccount.Trim(), paymentRequest.DebtorAccount.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The CreditorAccount and DebtorAccount cannot be the same.");
        }
        paymentRequest.ClientId = ClientIdAsInt;
        paymentRequest.Timestamp = DateTime.Now;
        await transactionHandler.ProcessTransaction(paymentRequest);
        return Results.Ok(paymentRequest);

    }
    catch(ArgumentException ex)
    {
       return Results.BadRequest(new { Error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict("Conflict: " + ex.Message);

    }
    catch (Exception ex) 
    {
        return Results.Conflict("Conflict: " + ex.Message);

    }


 

});


app.MapPost("/", async (HttpRequest request, [FromServices] TransactionHandler transactionHandler) =>
{


   if (!request.Headers.TryGetValue("ClientId", out var ClientId))
        {
            return Results.BadRequest("Missing required header: ClientHeader");
        }
    if (!int.TryParse(ClientId, out var ClientIdAsInt))
    {
        return Results.BadRequest("X-Custom-Header must be a valid integer.");
    }

    try
    {
        // Deserialize the request body into the object
        var requestBody = await JsonSerializer.DeserializeAsync<Payment>(request.Body);
        
        if (requestBody == null)
        {
            return Results.BadRequest("Invalid or missing request body.");
        }
        var  validationResults = new List<ValidationResult>();
       
        requestBody.ClientId = ClientIdAsInt;
        await transactionHandler.ProcessTransaction(requestBody);
        return Results.Ok(requestBody);

    }
    catch (Exception ex) { 
            return Results.BadRequest(ex.Message);  
    }
    // .}\..
});
app.Run();
