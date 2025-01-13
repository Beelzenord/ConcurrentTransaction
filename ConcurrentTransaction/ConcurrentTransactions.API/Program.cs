using ConcurrentTransactions.API.Channel;
using ConcurrentTransactions.API.Model;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using System.ComponentModel.DataAnnotations;

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


/// <summary>
/// Gets a list of successful transactions associated with this IBAN, if no other transaction is underway
/// </summary>
/// <returns> Returns a DateTime and List<Transaction> tuple </Transaction></returns>
app.MapGet("/accounts/{iban}/transactions", (string iban, TransactionHandler transactionHandler, CancellationToken cancellationToken) =>
{
    try
    {
        var (snapshotTime, transactions) = transactionHandler.GetSnapshotOfConfirmedTransactions(iban);

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

/// <summary>
/// Posts a payment re
/// </summary>
/// <returns> Returns a DateTime and List<Transaction> tuple </Transaction></returns>
app.MapPost("/payments", async (Payment paymentRequest, HttpRequest request, [FromServices] TransactionHandler transactionHandler) =>
{
    // Validate ClientId header
    if (!request.Headers.TryGetValue("ClientId", out var clientId) || !int.TryParse(clientId, out var clientIdAsInt))
    {
        throw new ArgumentException("Missing or invalid required header: ClientId.");
    }

    // Validate payment request
    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(paymentRequest, new ValidationContext(paymentRequest), validationResults, true))
    {
        throw new ValidationException($"Validation failed: {string.Join(", ", validationResults.Select(v => v.ErrorMessage))}");
    }

    // Check for identical accounts
    if (string.Equals(paymentRequest.CreditorAccount?.Trim(), paymentRequest.DebtorAccount?.Trim(), StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("The CreditorAccount and DebtorAccount cannot be the same.");
    }

    // Process transaction
    paymentRequest.ClientId = clientIdAsInt;
    paymentRequest.Timestamp = DateTime.Now;
    await transactionHandler.TryProcessNewTransaction(paymentRequest);

    return Results.Ok(paymentRequest);
});

//Custom API error handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ArgumentException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { Error = ex.Message });
    }
    catch (ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { ValidationErrors = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new { Conflict = ex.Message });
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            Error = "An unexpected error occurred.",
            Details = ex.Message
        });
    }
});
app.Run();
//Expose API for xunit
public partial class Program { }
