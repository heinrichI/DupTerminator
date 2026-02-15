using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.WPF.Model;
using DupTerminator.WPF.ViewModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static System.Formats.Asn1.AsnWriter;

namespace DupTerminator.WPF.Service
{
    public class WpfLogger : ILogger
    {
        private readonly ProgressDialogViewModel _viewModel;
        private readonly string _category;

        public WpfLogger(ProgressDialogViewModel viewModel, string category)
        {
            _viewModel = viewModel;
            _category = category;
        }

        // Holds the current stack of scopes for the async‑flow that is logging.
        private static readonly AsyncLocal<ScopeStack> _currentScope = new AsyncLocal<ScopeStack>();

        public IDisposable BeginScope<TState>(TState state)
        {
            // Create a new scope node and push it onto the stack.
            var newScope = new LoggerScope(state as IDictionary<string, object>);
            var stack = _currentScope.Value ?? new ScopeStack();
            stack.Push(newScope);
            _currentScope.Value = stack;
            return newScope;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                               Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            var logLevelString = logLevel.ToString();


            // Prepend the current scope chain (if any)
            var path = GetCurrentScopeString("Path");

            _viewModel.AddLogEntry(new LogEntry(DateTime.Now, logLevelString, message, _category, path));

            //if (exception != null)
            //{
            //    _viewModel.AddLogEntry(new LogEntry(DateTime.Now, LogLevel.Error.ToString(), exception.ToString(), _category));
            //}
        }

        /// <summary>
        /// Returns a string representation of the current scope chain,
        /// e.g. "ScopeA => ScopeB => ScopeC".
        /// </summary>
        private static string GetCurrentScopeString(string name)
        {
            var stack = _currentScope.Value;
            if (stack == null || stack.Count == 0)
                return string.Empty;

            // Build from outermost to innermost
            var parts = new List<string>(stack.Count);
            foreach (var scope in stack)
                parts.Add(scope.State?[name].ToString());

            return string.Join(" => ", parts);
        }

      

        /// <summary>
        /// Simple linked‑list stack that also implements IEnumerable for easy traversal.
        /// </summary>
        private sealed class ScopeStack : IEnumerable<LoggerScope>
        {
            private LoggerScope _head;
            public int Count { get; private set; }

            public void Push(LoggerScope scope)
            {
                scope.Next = _head;
                _head = scope;
                Count++;
            }

            public void Pop(LoggerScope scope)
            {
                // The scope being disposed should be the current head.
                // If it isn’t (e.g., because of misuse), we simply walk the list and remove it.
                if (_head == scope)
                {
                    _head = scope.Next;
                    Count--;
                    return;
                }

                var prev = _head;
                while (prev?.Next != null)
                {
                    if (prev.Next == scope)
                    {
                        prev.Next = scope.Next;
                        Count--;
                        break;
                    }
                    prev = prev.Next;
                }
            }

            public IEnumerator<LoggerScope> GetEnumerator()
            {
                var current = _head;
                while (current != null)
                {
                    yield return current;
                    current = current.Next;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        /// <summary>
        /// Represents a single logging scope. Disposing it removes it from the async‑flow stack.
        /// </summary>
        private sealed class LoggerScope : IDisposable
        {
            public IDictionary<string, object> State { get; }
            public LoggerScope? Next { get; set; }
            private bool _disposed;

            public LoggerScope(IDictionary<string, object> state)
            {
                State = state;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                var stack = _currentScope.Value;
                stack?.Pop(this);

                // If the stack becomes empty we clear the AsyncLocal to avoid memory leaks.
                if (stack != null && stack.Count == 0)
                    _currentScope.Value = null;
            }
        }

        private class LogScope : IDisposable
        {
            private static readonly AsyncLocal<LogScope> _current = new();
            private readonly LogScope _parent;
            private readonly string _state;

            public LogScope(string state)
            {
                _state = state;
                _parent = _current.Value;
                _current.Value = this;
            }

            public static string Current => _current.Value?.ToString();

            public void Dispose()
            {
                _current.Value = _parent;
            }

            public override string ToString()
            {
                if (_parent != null)
                {
                    return $"{_parent} => {_state}";
                }
                return _state;
            }
        }
    }
}
