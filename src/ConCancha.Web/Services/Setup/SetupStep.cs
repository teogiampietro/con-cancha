using ConCancha.Domain.Courts;

namespace ConCancha.Web.Services.Setup;

public sealed record SetupStep(SetupStepKind Kind, SportDraft? Sport = null);
