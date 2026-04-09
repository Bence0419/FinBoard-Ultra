namespace FinBoardUltra.Domain.Enums;

public enum AuditAction
{
    Register,
    Login,
    LoginFailed,
    Logout,
    PasswordChanged,
    MfaEnabled,
    MfaDisabled,
    MfaChallengeFailed,
    RecordCreated,
    RecordUpdated,
    RecordDeleted,
    CategoryCreated,
    CategoryDeleted,
    ProfileUpdated
}
