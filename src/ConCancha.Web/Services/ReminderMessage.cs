using ConCancha.Contracts.Reminders;

namespace ConCancha.Web.Services;

/// <summary>The WhatsApp text and link of a reminder. The club taps send: nothing leaves automatically.</summary>
public static class ReminderMessage
{
    private const string WhatsAppBase = "https://wa.me/";

    public static string Build(string clubName, ReminderDto reminder)
    {
        var firstName = reminder.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? reminder.CustomerName;
        return $"Hola {firstName}! Te recordamos tu turno en {clubName}: {DisplayFormat.LongDate(reminder.Date)} " +
               $"a las {DisplayFormat.Time(reminder.StartMinuteOfDay)} ({reminder.CourtName}). ¡Te esperamos!";
    }

    public static string WhatsAppUrl(string phone, string text) => $"{WhatsAppBase}{phone}?text={Uri.EscapeDataString(text)}";
}
