using ConCancha.Contracts.Bookings;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Customers;

namespace ConCancha.Web.Services;

/// <summary>The status chip shown next to a turn, the same everywhere it is listed.</summary>
public static class BookingPresentation
{
    public static string ChipText(BookingDto booking) =>
        ChipText(booking.Kind, booking.Status, booking.Attendance, booking.PaymentStatus);

    public static string ChipClass(BookingDto booking) =>
        ChipClass(booking.Kind, booking.Status, booking.Attendance, booking.PaymentStatus);

    public static string ChipText(BookingKind kind, BookingStatus status, Attendance attendance, PaymentStatus payment)
    {
        if (kind == BookingKind.Block) return "Bloqueo";
        if (status == BookingStatus.Cancelled) return "Cancelado";
        if (attendance == Attendance.NoShow) return "No vino";
        if (attendance == Attendance.Played) return "Jugado";
        return payment == PaymentStatus.Paid ? "Pagó" : "Debe";
    }

    public static string ChipClass(BookingKind kind, BookingStatus status, Attendance attendance, PaymentStatus payment)
    {
        if (kind == BookingKind.Block || status == BookingStatus.Cancelled) return string.Empty;
        if (attendance == Attendance.NoShow) return "warn";
        return attendance == Attendance.Played || payment == PaymentStatus.Paid ? "ok" : "warn";
    }

    /// <summary>The club taps send: nothing leaves automatically. Null when the phone is not a number WhatsApp can open.</summary>
    public static string? WhatsAppUrl(BookingDto booking)
    {
        if (PhoneNormalizer.Normalize(booking.CustomerPhone) is not { } phone) return null;

        var firstName = (booking.CustomerName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return ReminderMessage.WhatsAppUrl(phone, firstName is null ? "Hola!" : $"Hola {firstName}!");
    }
}
