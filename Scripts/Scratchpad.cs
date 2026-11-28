public closed record Node
{
    public sealed record Leaf(string Value) : Node;

    // Can hold any Node:
    public sealed record MixedBranch(IReadOnlyList<Node> Children) : Node;

    // Can restrict strictly to Leaf cases:
    public sealed record LeafOnlyBranch(IReadOnlyList<Leaf> Leaves) : Node;
}
