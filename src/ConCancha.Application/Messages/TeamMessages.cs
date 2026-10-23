namespace ConCancha.Application.Messages;

public static class TeamMessages
{
    public const string TeamFull = "Ya tenés el máximo de encargados. Quitá a alguien para sumar otra persona.";
    public const string AlreadyInTeam = "Esa persona ya está en tu equipo.";
    public const string MemberNotFound = "No encontramos a esa persona en tu equipo.";
    public const string InvitationNotFound = "No encontramos esa invitación.";
    public const string CannotRemoveOwner = "No se puede quitar al dueño del club.";
    public const string InvalidInvitation = "Este link de invitación no sirve o ya venció. Pedile al dueño que te invite de nuevo.";
    public const string InvitedAccountExists = "Ya hay una cuenta con ese mail. Pedile al dueño que te vuelva a invitar para sumarte.";
}
