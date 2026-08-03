using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Validates a dialog graph before export (Q15). Operates on the immutable core
    /// <see cref="IDialogGraph"/> (built from the editor's node views), so it is GraphView-free and
    /// unit-testable. Blocks stop export; warnings are advisory.
    ///
    /// Blocks: entry-node count != 1, an entry id that is not an EntryNode, any missing/dangling
    /// outgoing edge or dead-end, an empty ChoiceNode, and any condition/action type not in the
    /// registered sets. Warnings: unreachable/orphan nodes, and missing/empty localization keys.
    /// (Duplicate ids are rejected by <c>DialogGraph</c> construction; the exit-less auto-cycle
    /// warning is left to the runner's runtime guard for now.)
    /// </summary>
    public static class DialogValidator {
        public static ValidationReport Validate(
            IDialogGraph graph,
            IStringTable strings = null,
            ISet<string> knownConditionTypes = null,
            ISet<string> knownActionTypes = null) {

            var issues = new List<ValidationIssue>();
            if (graph == null) {
                issues.Add(ValidationIssue.Block("Graph is null."));
                return new ValidationReport(issues);
            }

            ValidateEntry(graph, issues);
            ValidateEdges(graph, issues);
            ValidateSpecTypes(graph, knownConditionTypes, knownActionTypes, issues);
            ValidateReachability(graph, issues);
            ValidateLocalization(graph, strings, issues);

            return new ValidationReport(issues);
        }

        private static void ValidateEntry(IDialogGraph graph, List<ValidationIssue> issues) {
            var entryCount = 0;
            foreach (var node in graph.Nodes) {
                if (node is EntryNode) {
                    entryCount++;
                }
            }
            if (entryCount == 0) {
                issues.Add(ValidationIssue.Block("The graph has no EntryNode."));
            } else if (entryCount > 1) {
                issues.Add(ValidationIssue.Block($"The graph has {entryCount} EntryNodes; exactly one is required."));
            }

            if (!(graph.Find(graph.EntryId) is EntryNode)) {
                issues.Add(ValidationIssue.Block($"Entry id '{graph.EntryId}' does not point to an EntryNode."));
            }
        }

        private static void ValidateEdges(IDialogGraph graph, List<ValidationIssue> issues) {
            foreach (var node in graph.Nodes) {
                if (node is ChoiceNode choice && choice.Options.Count == 0) {
                    issues.Add(ValidationIssue.Block("ChoiceNode has no options.", node.Id));
                }

                foreach (var target in OutgoingTargets(node)) {
                    if (string.IsNullOrEmpty(target)) {
                        issues.Add(ValidationIssue.Block("Node has a missing outgoing edge (dead-end).", node.Id));
                    } else if (graph.Find(target) == null) {
                        issues.Add(ValidationIssue.Block($"Edge points to a missing node '{target}'.", node.Id));
                    }
                }
            }
        }

        private static void ValidateSpecTypes(IDialogGraph graph, ISet<string> knownConditions,
            ISet<string> knownActions, List<ValidationIssue> issues) {
            foreach (var node in graph.Nodes) {
                switch (node) {
                    case LineNode line:
                        CheckActions(line.OnEnter, knownActions, node.Id, issues);
                        break;
                    case ActionNode action:
                        CheckActions(action.Actions, knownActions, node.Id, issues);
                        break;
                    case ConditionNode condition:
                        CheckCondition(condition.Condition, knownConditions, node.Id, issues);
                        break;
                    case ChoiceNode choice:
                        foreach (var option in choice.Options) {
                            CheckCondition(option.Show, knownConditions, node.Id, issues);
                            CheckActions(option.OnSelect, knownActions, node.Id, issues);
                        }
                        break;
                }
            }
        }

        private static void ValidateReachability(IDialogGraph graph, List<ValidationIssue> issues) {
            var reachable = new HashSet<string>();
            var frontier = new Queue<string>();
            if (graph.Find(graph.EntryId) != null) {
                frontier.Enqueue(graph.EntryId);
                reachable.Add(graph.EntryId);
            }
            while (frontier.Count > 0) {
                var node = graph.Find(frontier.Dequeue());
                if (node == null) {
                    continue;
                }
                foreach (var target in OutgoingTargets(node)) {
                    if (!string.IsNullOrEmpty(target) && graph.Find(target) != null && reachable.Add(target)) {
                        frontier.Enqueue(target);
                    }
                }
            }

            foreach (var node in graph.Nodes) {
                if (!reachable.Contains(node.Id)) {
                    issues.Add(ValidationIssue.Warn("Node is unreachable from the entry.", node.Id));
                }
            }
        }

        private static void ValidateLocalization(IDialogGraph graph, IStringTable strings, List<ValidationIssue> issues) {
            if (strings == null) {
                return;
            }
            foreach (var node in graph.Nodes) {
                switch (node) {
                    case LineNode line:
                        CheckKey(line.TextKey, strings, node.Id, issues);
                        break;
                    case ChoiceNode choice:
                        foreach (var option in choice.Options) {
                            CheckKey(option.TextKey, strings, node.Id, issues);
                        }
                        break;
                }
            }
        }

        private static IEnumerable<string> OutgoingTargets(IDialogNode node) {
            switch (node) {
                case EntryNode entry:
                    yield return entry.Next;
                    break;
                case LineNode line:
                    yield return line.Next;
                    break;
                case ActionNode action:
                    yield return action.Next;
                    break;
                case ConditionNode condition:
                    yield return condition.IfTrue;
                    yield return condition.IfFalse;
                    break;
                case ChoiceNode choice:
                    foreach (var option in choice.Options) {
                        yield return option.Target;
                    }
                    break;
                // ExitNode: terminal, no outgoing edges.
            }
        }

        private static void CheckActions(IReadOnlyList<ActionSpec> actions, ISet<string> known,
            string nodeId, List<ValidationIssue> issues) {
            if (known == null || actions == null) {
                return;
            }
            foreach (var action in actions) {
                if (action != null && !known.Contains(action.Type)) {
                    issues.Add(ValidationIssue.Block($"Unregistered action type '{action.Type}'.", nodeId));
                }
            }
        }

        private static void CheckCondition(ConditionSpec condition, ISet<string> known,
            string nodeId, List<ValidationIssue> issues) {
            if (known == null || condition == null) {
                return;
            }
            if (!known.Contains(condition.Type)) {
                issues.Add(ValidationIssue.Block($"Unregistered condition type '{condition.Type}'.", nodeId));
            }
        }

        private static void CheckKey(string key, IStringTable strings, string nodeId, List<ValidationIssue> issues) {
            if (string.IsNullOrEmpty(key) || !strings.TryGet(key, out var text) || string.IsNullOrEmpty(text)) {
                issues.Add(ValidationIssue.Warn($"Missing or empty localized text for key '{key}'.", nodeId));
            }
        }
    }
}
