namespace Dependify.Core.Graph;

using System.Globalization;

public sealed record Edge(Node Start, Node End, string Label)
{
    public Node Start { get; } = Start ?? throw new ArgumentNullException(nameof(Start));
    public Node End { get; } = End ?? throw new ArgumentNullException(nameof(End));
    public string Label { get; } = Label;

    public Edge(Node start, Node end)
        : this(start, end, string.Empty) { }

    public override string ToString() =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0} -{2}-> {1}",
            this.Start,
            this.End,
            string.IsNullOrEmpty(this.Label) ? string.Empty : $"[{this.Label}]"
        );
}
