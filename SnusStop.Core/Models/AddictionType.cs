namespace SnusStop.Core.Models;

/// <summary>
/// What the user is quitting. Every unit and every piece of user-facing copy that differs between
/// them is resolved through <see cref="AddictionCopy"/> — never by branching in the UI.
/// </summary>
/// <remarks>
/// Snus is deliberately 0: profiles saved before this setting existed read back as Snus, which is
/// what the app used to be.
/// </remarks>
public enum AddictionType
{
    Snus = 0,
    Cigarettes = 1,
}
