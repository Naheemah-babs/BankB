
using TatumWelcome2.BLL;
using TatumWelcome2.Controllers;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;
using TatumWelcome2.Presentation;
using TatumWelcome2.Repositories;
using TatumWelcome2.Services;
using System.Globalization;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

CultureInfo.DefaultThreadCurrentCulture =
    CultureInfo.GetCultureInfo("en-NG");

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter(
    "System.Net.Http.HttpClient",
    LogLevel.Warning);

builder.Logging.AddFilter(
    "Microsoft.AspNetCore",
    LogLevel.Warning);

// App B API address.
builder.WebHost.UseUrls("http://localhost:5001");

// Shared in-memory account list.
List<Account> accounts = AccountData.GetAccounts();

// Repository.
builder.Services.AddSingleton<IAccountRepository>(
    new AccountRepository(accounts));

// HTTP communication.
builder.Services.AddHttpClient<IBankApiClient, BankApiClient>();

// Business logic services.
builder.Services.AddSingleton<InterbankTransferService>();
builder.Services.AddSingleton<InterbankOutgoingTransferService>();
builder.Services.AddSingleton(new AdminService(accounts));

// Automatic pending-payment reconciliation.
builder.Services.AddHostedService<BillPaymentReconciliationService>();

var app = builder.Build();

// Resolve services.
IBankApiClient bankApiClient =
    app.Services.GetRequiredService<IBankApiClient>();

InterbankOutgoingTransferService outgoingTransferService =
    app.Services.GetRequiredService<InterbankOutgoingTransferService>();

AdminService adminService =
    app.Services.GetRequiredService<AdminService>();

// API endpoints.
app.MapGet(
    "/health",
    () => "Tatum Bank API is running");

app.MapAccountEndpoints();
app.MapInterbankTransferEndpoints();

// Start API and background services.
var serverTask = app.RunAsync();

try
{
    await BankApp.RunAsync(
        accounts,
        bankApiClient,
        outgoingTransferService,
        adminService,
        app.Configuration);
}
finally
{
    await app.StopAsync();
    await serverTask;
}
