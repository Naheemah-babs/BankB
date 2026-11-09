
using TatumWelcome2.BLL;
using TatumWelcome2.DTOs;

namespace TatumWelcome2.Controllers;

public static class InterbankTransferEndpoints
{
    public static void MapInterbankTransferEndpoints(
        this WebApplication app)
    {
        app.MapPost(
            "/transfers/receive",
            (InterbankTransferRequestDto request,
             InterbankTransferService service) =>
            {
                string result = service.ReceiveTransfer(request);

                return result switch
                {
                    "Success" =>
                        Results.Ok(
                            "Transfer received successfully."),

                    "AlreadyProcessed" =>
                        Results.Ok(
                            "Transfer already processed."),

                    "AccountNotFound" =>
                        Results.NotFound(
                            "Recipient account not found."),

                    "ReferenceConflict" =>
                        Results.Conflict(
                            "Transfer reference conflict."),

                    _ =>
                        Results.BadRequest(
                            "Invalid transfer request.")
                };
            });

        app.MapGet(
            "/transfers/status/{reference}",
            (string reference,
             InterbankTransferService service) =>
            {
                InterbankTransferRequestDto? transfer =
                    service.FindProcessedTransfer(reference);

                if (transfer == null)
                {
                    return Results.NotFound(
                        "No processed transfer found for this reference.");
                }

                return Results.Ok(transfer);
            });
    }
}
