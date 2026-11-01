namespace TelematrackFunctions;

/// <summary>
/// CONTRACT: infra/alerts.tf searches the logs for the literal text "ALERT Critical".
/// The severity word is part of the template on purpose, so the text is present
/// whether the log pipeline stores the template or the formatted message.
/// </summary>
public static class AlertMessages
{
    public const string CriticalMarker = "ALERT Critical";

    public const string CriticalTemplate =
        "ALERT Critical on {VehicleId}: code={FaultCode}, oilTemp={Oil}";

    public const string WarningTemplate =
        "ALERT Warning on {VehicleId}: code={FaultCode}, oilTemp={Oil}";
}