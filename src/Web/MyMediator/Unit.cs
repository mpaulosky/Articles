namespace Web.MyMediator;

/// <summary>
///     Represents a request or notification result that carries no data, for use in place of <see langword="void" />
///     in generic handler signatures.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
	public static readonly Unit Value;

	public bool Equals(Unit other) => true;

	public override bool Equals(object? obj) => obj is Unit;

	public override int GetHashCode() => 0;

	public override string ToString() => "()";

	public static bool operator ==(Unit left, Unit right) => true;

	public static bool operator !=(Unit left, Unit right) => false;
}
