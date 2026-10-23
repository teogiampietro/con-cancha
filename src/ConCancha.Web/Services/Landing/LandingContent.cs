namespace ConCancha.Web.Services.Landing;

public static class LandingContent
{
    public const string TrialButton = "Probalo gratis 14 días";
    public const string TrialPlanButton = "Empezar mis 14 días gratis";
    public const string ExampleClubLink = "concancha.com/nombre-del-club";
    public const string NotifyContactAddress = "hola@concancha.com";
    public const string PaidPlanName = "Plan Completo";
    public const string PaidPlanBadge = "Recomendado";
    public const string PaidPlanTagline = "Se paga solo con un par de turnos que antes quedaban vacíos.";
    public const string PaidPlanIntro = "Todo lo del plan de prueba, más:";

    public static readonly IReadOnlyList<string> SloganLines = ["Más deportes.", "Más momentos.", "ConCancha."];

    public static readonly PaidPlan Plan = new(
        Price: "$50.000",
        Period: "mes",
        Includes:
        [
            "Gestión de torneos y ligas para llenar tus canchas",
            "Gestión de bar/buffet, junto a la caja del club",
            "Atención personalizada por WhatsApp, con una persona real",
            "Asistente que responde las consultas de tus jugadores las 24 hs"
        ]);

    public static readonly IReadOnlyList<LandingItem> ClubFeatures =
    [
        new("calendar", "Agenda por cancha y hora", "Cargás turnos, bloqueás horarios por lluvia o mantenimiento y movés turnos dentro del mismo día. Marcás si se jugó o si no vino, y lo podés deshacer."),
        new("grid", "Canchas y precios", "Cada cancha con su deporte y la duración del turno: pádel 90 min, fútbol 60 min. Precios por franja: “viernes a domingo más caro” o “desde las 18 hs”."),
        new("wallet", "Cobros y Caja", "Cada turno figura como Debe o Pagó, en efectivo o transferencia. La Caja del día muestra lo cobrado por método y lo que falta cobrar. Se imprime."),
        new("users", "Clientes", "Visitas, gasto, deuda y faltas de cada cliente. Filtrás por quienes deben o faltaron y exportás a Excel o PDF."),
        new("list", "Reservas", "Filtrás por hoy, 7 días, 30 días o historial, por “a cobrar” o por “online”. Buscás por nombre o teléfono."),
        new("bell", "Recordatorios por WhatsApp", "La app arma el mensaje de “mañana” y “hoy”. Lo abrís con un toque."),
        new("shield", "Roles", "Dueño y encargado. El encargado carga turnos y cobra, pero no toca la configuración."),
        new("clock", "Reglas de cancelación", "Vos definís cuántas horas antes puede cancelar el jugador. Por defecto, 12 hs.")
    ];

    public static readonly IReadOnlyList<LandingItem> PlayerFeatures =
    [
        new("calendar", "Elige día, cancha y horario", "Ve los próximos 7 días, la cancha y el horario libre. Y el precio, antes de reservar."),
        new("users", "Reserva sin cuenta", "Con nombre y WhatsApp alcanza. El mail es opcional. No descarga nada."),
        new("clock", "Cancela dentro de la ventana", "Ve sus turnos y los cancela dentro del plazo que fijó el club, o con el link personal que recibe al reservar.")
    ];

    public static readonly IReadOnlyList<LandingItem> OrderRules =
    [
        new("shield", "No hay doble reserva", "Dos personas no pueden quedarse con el mismo horario, ni siquiera si reservan al mismo tiempo."),
        new("bell", "Máximo 2 turnos por delante", "Cada persona puede tener hasta 2 turnos pendientes. Frena las reservas falsas.")
    ];

    public static readonly IReadOnlyList<string> NotebookPains =
    [
        "Mensajes de “¿tenés algo para hoy?” a cualquier hora",
        "Turnos anotados en el cuaderno, la planilla y el chat",
        "Dos personas con el mismo horario",
        "Turnos que se caen y nadie se entera",
        "La caja que no cierra"
    ];

    public static readonly IReadOnlyList<string> ConCanchaGains =
    [
        "El jugador ve lo libre y reserva solo, también de noche",
        "Una sola agenda por cancha y por hora",
        "Un horario no se puede reservar dos veces",
        "Recordatorios, cancelación con plazo y tope de turnos",
        "Caja del día que cuadra con lo que cargaste"
    ];

    public static readonly IReadOnlyList<LandingItem> Benefits =
    [
        new("chat", "Se acabó el ida y vuelta por WhatsApp", "El jugador ve lo libre y reserva solo, también de noche y los fines de semana."),
        new("bell", "Menos turnos que se caen", "Hay recordatorios, ventana de cancelación y límite de turnos por persona. Los “no vino” quedan registrados."),
        new("chart", "Ves cuánta plata se te escapa", "Los horarios libres que pasaron se convierten en un número, y la Caja cuadra con lo que cargaste."),
        new("rocket", "Se arranca en minutos", "Sin tarjeta, sin instalar nada, y funciona en el celular de todos.")
    ];

    public static readonly IReadOnlyList<string> TrialIncludes =
    [
        "Sin tarjeta de crédito y sin compromiso",
        "Acceso completo para configurar el club",
        "Cargá turnos y recibí reservas online",
        "Al terminar, elegís un plan pago"
    ];

    public static readonly IReadOnlyList<LandingItem> ChatbotFeatures =
    [
        new("chat", "Pestaña “Preguntar” en la página del club", "El jugador escribe como en WhatsApp: “¿Hay pádel el sábado a la tarde?” o “¿Cuánto sale la cancha de fútbol?”."),
        new("clock", "Responde al instante", "Dice qué canchas y horarios hay libres y cuánto cuestan, sin esperar a que alguien del club conteste."),
        new("calendar", "Lo lleva a reservar", "Después de la respuesta, el jugador toca el horario y completa la reserva."),
        new("bell", "El club no atiende consultas repetidas", "Se acaban los mensajes de “¿tenés algo para hoy?”."),
        new("chart", "Un asistente para el dueño", "Cuenta cómo viene el día, qué hay libre y a quién hay que cobrarle.")
    ];

    public static readonly IReadOnlyList<LandingFaq> Faqs =
    [
        new("¿Necesito tarjeta de crédito para probarlo?", "No. Los 14 días de prueba son sin tarjeta y sin compromiso. Al terminar, elegís un plan pago."),
        new("¿Qué pasa con los datos de mi club?", "Cada club ve solo lo suyo. Además hay roles: el dueño configura y el encargado carga turnos y cobra."),
        new("¿Cuántas canchas puedo cargar?", "Cargás las canchas de tu club, cada una con su deporte, la duración del turno y sus precios. [A COMPLETAR POR EL DUEÑO: límite de canchas por plan, si lo hay]"),
        new("¿El jugador necesita crear una cuenta?", "No. Reserva con nombre y WhatsApp (el mail es opcional) y no descarga nada. Después ve y cancela sus turnos desde la página del club o con el link personal que recibe."),
        new("¿Tengo que instalar algo?", "No. Funciona en el navegador del celular, tanto para el club como para los jugadores.")
    ];
}
