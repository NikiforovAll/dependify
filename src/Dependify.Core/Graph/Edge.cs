namespace Dependify.Core.Graph;

using System.Globalization;

public sealed record Edge(Node Start, Node End)
{
    public Node Start { get; } = Start ?? throw new ArgumentNullException(nameof(Start));
    public Node End { get; } = End ?? throw new ArgumentNullException(nameof(End));

    /// <inheritdoc/>
    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "{0} -> {1}", this.Start, this.End);
}
