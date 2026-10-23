using ConCancha.Domain.Clubs;

namespace ConCancha.Tests.Domain;

public class TeamPolicyTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(TeamPolicy.MaxStaffPerClub - 1, true)]
    [InlineData(TeamPolicy.MaxStaffPerClub, false)]
    public void HasRoomForAnother_StopsAtTheLimit(int occupied, bool expected) =>
        Assert.Equal(expected, TeamPolicy.HasRoomForAnother(occupied));

    [Fact]
    public void Invitation_IsPendingOnlyUntilItExpires()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = new ClubInvitation { ExpiresAt = TeamPolicy.ExpiryFrom(now) };

        Assert.True(invitation.IsPendingAt(now));
        Assert.False(invitation.IsPendingAt(now + TeamPolicy.InvitationValidity));
    }

    [Theory]
    [InlineData(InvitationStatus.Accepted)]
    [InlineData(InvitationStatus.Revoked)]
    public void Invitation_IsNotPendingOnceUsedOrRevoked(InvitationStatus status)
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = new ClubInvitation { Status = status, ExpiresAt = TeamPolicy.ExpiryFrom(now) };

        Assert.False(invitation.IsPendingAt(now));
    }
}
