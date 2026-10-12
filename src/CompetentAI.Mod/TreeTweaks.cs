using System;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using ThunderRoad.AI;
using ThunderRoad.AI.Action;

namespace CompetentAI.Mod
{
    /// <summary>
    /// Finds the <see cref="AttackMelee"/> nodes inside a creature's OWN copy of its behaviour tree (every creature
    /// deserialises its own tree and child trees, so editing a node never affects other creatures or the catalog).
    /// </summary>
    internal static class TreeTweaks
    {
        private static readonly FieldInfo ChildTreeField =
            typeof(ChildTreeNode).GetField("childTree", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void FindAttackNodes(BehaviorTreeData tree, List<AttackMelee> into)
        {
            into.Clear();
            if (tree == null) return;
            var seen = new HashSet<object>();
            Walk(tree.rootNode, into, seen, 0);
        }

        private static void Walk(Node node, List<AttackMelee> into, HashSet<object> seen, int depth)
        {
            if (node == null || depth > 40 || !seen.Add(node)) return;
            if (node is AttackMelee melee)
            {
                into.Add(melee);
                return;
            }
            if (node is ControlNode control)
            {
                for (int i = 0; i < control.childs.Count; i++) Walk(control.childs[i], into, seen, depth + 1);
            }
            else if (node is DecoratorNode decorator)
            {
                Walk(decorator.child, into, seen, depth + 1);
            }
            else if (node is ChildTreeNode childNode && ChildTreeField != null)
            {
                var child = ChildTreeField.GetValue(childNode) as BehaviorTreeData;
                if (child != null && seen.Add(child)) Walk(child.rootNode, into, seen, depth + 1);
            }
        }
    }
}
