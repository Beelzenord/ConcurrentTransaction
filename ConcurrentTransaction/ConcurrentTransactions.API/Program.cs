using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ConcurrentTransactions.API.Channel;
using ConcurrentTransactions.API.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

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



app.MapGet("/accounts/{iban}/transactions", async (string iban, TransactionHandler transactionHandler, CancellationToken cancellationToken) =>
{
    try
    {
        var (snapshotTime, transactions) = await transactionHandler.GetSnapshotOfConfirmedTransactions(iban);

        return Results.Ok(new SnapshotResponse
        {
            SnapshotTime = snapshotTime,
            Transactions = transactions
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.NoContent();
    }
});


app.MapPost("/payments", async (Payment paymentRequest, HttpRequest request, [FromServices] TransactionHandler transactionHandler) =>
{
    if (!request.Headers.TryGetValue("ClientId", out var clientId) || !int.TryParse(clientId, out var clientIdAsInt))
    {
        return Results.BadRequest(new { Error = "Missing or invalid required header: ClientId." });
    }

    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(paymentRequest, new ValidationContext(paymentRequest), validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(v => new
        {
            Field = v.MemberNames.FirstOrDefault() ?? "General",
            Error = v.ErrorMessage
        }));
    }

    if (string.Equals(paymentRequest.CreditorAccount?.Trim(), paymentRequest.DebtorAccount?.Trim(), StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { BadRequest = "The CreditorAccount and DebtorAccount cannot be the same." });
    }

    try
    {
        paymentRequest.ClientId = clientIdAsInt;
        paymentRequest.Timestamp = DateTime.UtcNow; // Use UTC for consistency in timestamps
        await transactionHandler.ProcessTransaction(paymentRequest);
        return Results.Ok(paymentRequest);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { Conflict = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.BadRequest( new { Error = "An unexpected error occurred.", Details = ex.Message });
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
public partial class Program { }
