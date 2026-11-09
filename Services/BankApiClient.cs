
using System.Net;
using System.Net.Http.Json;
using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;

namespace TatumWelcome2.Services;

public class BankApiClient : IBankApiClient
{
    private readonly HttpClient _httpClient;

    public BankApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<AccountLookupDto?> FindAccountAsync(
        string bankUrl,
        string accountNumber)
    {
        string url =
            $"{bankUrl.TrimEnd('/')}/accounts/{accountNumber}";

        using HttpResponseMessage response =
            await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<AccountLookupDto>();
    }

    public Task<HttpResponseMessage> SendTransferAsync(
        string bankUrl,
        InterbankTransferRequestDto request)
    {
        string url =
            $"{bankUrl.TrimEnd('/')}/transfers/receive";

        return _httpClient.PostAsJsonAsync(url, request);
    }

    public Task<HttpResponseMessage> GetTransferStatusAsync(
        string bankUrl,
        string reference)
    {
        string url =
            $"{bankUrl.TrimEnd('/')}/transfers/status/" +
            Uri.EscapeDataString(reference);

        return _httpClient.GetAsync(url);
    }

    public Task<HttpResponseMessage> SendBillPaymentAsync(
        string bankUrl,
        BillPaymentRequestDto request)
    {
        string url =
            $"{bankUrl.TrimEnd('/')}/bills/pay";

        return _httpClient.PostAsJsonAsync(url, request);
    }
}
