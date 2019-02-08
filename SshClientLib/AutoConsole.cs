using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;

namespace SshClient {
    public class AutoConsole : Console {
        public bool IsFinished {
            get; protected set;
        } = false;
        IEnumerator<string> ValueEnumerator;
        public AutoConsole(string HostName, string UserName, string Password, Encoding Encoding, string EndPattern, IEnumerable<string> ValueEnumerable)
            : base(HostName, UserName, Password, Encoding, EndPattern, true)
            => ValueEnumerator = ValueEnumerable?.GetEnumerator() ?? throw new ArgumentNullException(nameof(ValueEnumerable));


        public AutoConsole(ConnectionInfo ConnectionInfo, string EndPattern, IEnumerable<string> ValueEnumerable) : base(ConnectionInfo, EndPattern, true)
            => ValueEnumerator = ValueEnumerable?.GetEnumerator() ?? throw new ArgumentNullException(nameof(ValueEnumerable));
        protected override Task<string> NextValueAsync(CancellationToken Token = default)
        {
            if (!(ValueEnumerator?.MoveNext() ?? false)) {
                IsFinished = true;
                return Task.FromResult<string>(null);
            }
            var Value = ValueEnumerator.Current;
            return Task.FromResult(Value);
        }
        #region IDisposable Support
        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue) {
                if (disposing) {
                    ValueEnumerator.Dispose();
                }
                ValueEnumerator = null;
                disposedValue = true;
            }
        }
        #endregion
    }
}