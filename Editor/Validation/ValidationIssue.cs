using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>How severe a validation issue is. Blocks stop export; warnings do not (Q15).</summary>
    public enum ValidationSeverity {
        Warning,
        Block
    }

    /// <summary>A single validation finding, optionally pinned to a node.</summary>
    public sealed class ValidationIssue {
        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public string NodeId { get; }

        public ValidationIssue(ValidationSeverity severity, string message, string nodeId = null) {
            Severity = severity;
            Message = message;
            NodeId = nodeId;
        }

        public static ValidationIssue Block(string message, string nodeId = null)
            => new ValidationIssue(ValidationSeverity.Block, message, nodeId);

        public static ValidationIssue Warn(string message, string nodeId = null)
            => new ValidationIssue(ValidationSeverity.Warning, message, nodeId);

        public override string ToString()
            => NodeId == null ? $"[{Severity}] {Message}" : $"[{Severity}] {Message} (node '{NodeId}')";
    }

    /// <summary>The result of validating a graph: the issues, and whether export is allowed.</summary>
    public sealed class ValidationReport {
        public IReadOnlyList<ValidationIssue> Issues { get; }

        public ValidationReport(IReadOnlyList<ValidationIssue> issues) {
            Issues = issues ?? new List<ValidationIssue>();
        }

        public bool HasBlocks {
            get {
                foreach (var issue in Issues) {
                    if (issue.Severity == ValidationSeverity.Block) {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>True when there are no blocking issues (warnings are allowed).</summary>
        public bool CanExport => !HasBlocks;
    }
}
