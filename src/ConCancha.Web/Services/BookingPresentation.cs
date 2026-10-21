using ConCancha.Contracts.Bookings;
using ConCancha.Domain.Bookings;

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
}
