using System;
using System.Collections.Generic;
using System.Linq;
using DataWF.Invokers;
using DataWF.Invokers.Extensions;
using NUnit.Framework;

namespace DataWF.Test.Invokers;

[TestFixture]
public partial class GroupExtensionsTests
{
    [Test]
    public void ParentHelpersAndExpansionWorkForAGroupTree()
    {
        var root = new GroupNode("root") { IsExpanded = true };
        var division = root.AddChild("division");
        var team = division.AddChild("team");
        var employee = team.AddChild("employee");

        Assert.That(employee.Level(), Is.EqualTo(3));
        Assert.That(employee.TopGroup(out var level), Is.SameAs(root));
        Assert.That(level, Is.EqualTo(3));
        Assert.That(employee.IsParent(division), Is.True);
        Assert.That(employee.IsParent(employee), Is.False);
        Assert.That(employee.GetAllParent(), Is.EquivalentTo(new[] { team, division, root }));
        Assert.That(employee.GetAllParent(addSender: true), Is.EquivalentTo(new[] { employee, team, division, root }));
        Assert.That(employee.GetFullName(separator: "/"), Is.EqualTo("root/division/team/employee"));

        Assert.That(employee.IsAllGroupsExpand(GroupNode.GroupInvoker.Instance, GroupNode.IsExpandedInvoker.Instance), Is.False);
        employee.ExpandAll(expand: true);

        Assert.That(root.IsExpanded, Is.True);
        Assert.That(division.IsExpanded, Is.True);
        Assert.That(team.IsExpanded, Is.True);
        Assert.That(employee.IsExpanded, Is.False);
        Assert.That(employee.IsAllGroupsExpand(GroupNode.GroupInvoker.Instance, GroupNode.IsExpandedInvoker.Instance), Is.True);
    }

    [Test]
    public void ParentTraversalsStopAtCycles()
    {
        var first = new GroupNode("first");
        var second = new GroupNode("second");
        first.Group = second;
        second.Group = first;

        Assert.That(first.Level(), Is.EqualTo(1));
        Assert.That(first.IsParent(second), Is.True);
        Assert.That(first.IsParent(first), Is.False);
        Assert.That(first.GetAllParent(), Is.EquivalentTo(new[] { second }));
        Assert.That(first.GetFullName(separator: "/"), Is.EqualTo("second/first"));
        Assert.That(first.IsAllGroupsExpand(GroupNode.GroupInvoker.Instance, GroupNode.IsExpandedInvoker.Instance), Is.False);

        first.ExpandAll(expand: true);
        Assert.That(first.IsExpanded, Is.False);
        Assert.That(second.IsExpanded, Is.True);
    }

    [Test]
    public void GetSubGroupsReturnsDescendantsOnceAndOptionallyIncludesSender()
    {
        var root = new GroupNode("root");
        var left = root.AddChild("left");
        var right = root.AddChild("right");
        var leaf = left.AddChild("leaf");
        right.Children.Add(root);

        Assert.That(root.GetSubGroups(GroupNode.ChildrenInvoker.Instance), Is.EquivalentTo(new[] { left, right, leaf }));
        Assert.That(root.GetSubGroups(GroupNode.ChildrenInvoker.Instance, addSender: true), Is.EquivalentTo(new[] { root, left, right, leaf }));
    }

    [Test]
    public void SortAndCompareOrderNodesByTheirGroupRoots()
    {
        var rootA = new GroupNode("A");
        var childA = rootA.AddChild("childA");
        var subChildA = childA.AddChild("subChildA");
        var rootB = new GroupNode("B");
        var childB = rootB.AddChild("childB");

        Assert.That(GroupExtensions.Compare(childA, rootB), Is.LessThan(0));
        Assert.That(GroupExtensions.Compare(rootB, childA), Is.GreaterThan(0));
        Assert.That(GroupExtensions.Compare(rootA, rootA), Is.Zero);
        Assert.That(GroupExtensions.Compare(null, rootA), Is.LessThan(0));
        Assert.That(GroupExtensions.Compare(rootA, null), Is.GreaterThan(0));
        Assert.That(
            GroupExtensions.Sort(new[] { childB, rootB, childA, rootA, subChildA }).Select(node => node.Name),
            Is.EqualTo(new[] { "A", "childA", "subChildA", "B", "childB" }));
    }

    [Test]
    public void AntiLoopSetRejectsRepeatedItems()
    {
        var visited = new GroupExtensions.AntiLoopSet<int>();
        Assert.That(visited.TryPush(1), Is.True);
        Assert.That(visited.TryPush(2), Is.True);
        Assert.That(visited.TryPush(3), Is.True);
        Assert.That(visited.TryPush(4), Is.True);
        Assert.That(visited.TryPush(5), Is.True);
        Assert.That(visited.TryPush(6), Is.True);
        Assert.That(visited.TryPush(7), Is.True);
        Assert.That(visited.TryPush(1), Is.False);
        Assert.That(visited.TryPush(2), Is.False);
        Assert.That(visited.Count, Is.EqualTo(7));
    }

    [Invoker(Instance = true)]
    private sealed partial class GroupNode(string name) : IComparable<GroupNode>, IEquatable<GroupNode>
    {
        public string Name { get; } = name;
        public GroupNode? Group { get; set; }
        public bool IsExpanded { get; set; }
        public List<GroupNode> Children { get; } = [];

        public GroupNode AddChild(string childName)
        {
            var child = new GroupNode(childName) { Group = this };
            Children.Add(child);
            return child;
        }

        public int CompareTo(GroupNode? other) =>
            string.Compare(Name, other?.Name, StringComparison.Ordinal);

        public bool Equals(GroupNode? groupNode) => string.Equals(Name, groupNode?.Name, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as GroupNode);

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => Name;
    }
}
