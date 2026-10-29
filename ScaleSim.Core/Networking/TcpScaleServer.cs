using System.Net;
using System.Net.Sockets;
using System.Text;
using ScaleSim.Core.Abstractions;

namespace ScaleSim.Core.Networking;

public sealed record ClientInfo(string Id, string Remote, DateTime Since);

/// <summary>
/// Raw TCP listener for one simulated scale. It only does framing (lines ending in LF, CR stripped)
/// and logging; what a line means is the protocol's business. Bytes are mapped 1:1 through Latin-1
/// so whatever a client sends is echoed to the log untouched.
/// </summary>
public sealed class TcpScaleServer
{
    private readonly IScaleProtocol _protocol;
    private readonly ActivityLog _log;
    private readonly object _gate = new();
    private readonly Dictionary<string, ClientChannel> _clients = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private int _nextClient;

    public TcpScaleServer(IScaleProtocol protocol, ActivityLog log)
    {
        _protocol = protocol;
        _log = log;
    }

    public event Action? ClientsChanged;

    public bool IsListening => _listener is not null;

    public IReadOnlyList<ClientInfo> Clients
    {
        get { lock (_gate) return _clients.Values.Select(c => new ClientInfo(c.Id, c.Remote, c.Since)).ToList(); }
    }

    /// <summary>Starts listening on all interfaces. Returns an error message, or null on success.</summary>
    public string? Start(int port)
    {
        Stop();
        try
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            _listener = listener;
            _cts = new CancellationTokenSource();
            _ = AcceptLoopAsync(listener, _cts.Token);
            _log.Add(ActivityKind.Info, "", $"Escuchando en el puerto {port}");
            return null;
        }
        catch (SocketException ex)
        {
            var msg = ex.SocketErrorCode == SocketError.AddressAlreadyInUse
                ? $"El puerto {port} ya está en uso por otra aplicación."
                : $"No se pudo abrir el puerto {port}: {ex.Message}";
            _log.Add(ActivityKind.Error, "", msg);
            return msg;
        }
    }

    public void Stop()
    {
        var cts = _cts; var listener = _listener;
        _cts = null; _listener = null;
        if (cts is null && listener is null) return;
        cts?.Cancel();
        try { listener?.Stop(); } catch { /* already closed */ }
        List<ClientChannel> open;
        lock (_gate) open = _clients.Values.ToList();
        foreach (var c in open) c.Close();
        _log.Add(ActivityKind.Info, "", "Servidor detenido");
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient tcp;
            try { tcp = await listener.AcceptTcpClientAsync(ct); }
            catch { return; }
            _ = RunClientAsync(tcp, ct);
        }
    }

    private async Task RunClientAsync(TcpClient tcp, CancellationToken serverCt)
    {
        tcp.NoDelay = true;
        var remote = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
        var channel = new ClientChannel($"c{Interlocked.Increment(ref _nextClient)}", remote, tcp, _log);
        lock (_gate) _clients[channel.Id] = channel;
        _log.Add(ActivityKind.Info, remote, "Cliente conectado");
        ClientsChanged?.Invoke();

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(serverCt, channel.Token);
        var session = _protocol.OpenSession(channel);
        try
        {
            var buffer = new byte[4096];
            var pending = new StringBuilder();
            var stream = tcp.GetStream();
            while (!linked.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, linked.Token);
                if (read == 0) break;
                pending.Append(Encoding.Latin1.GetString(buffer, 0, read));
                int nl;
                while ((nl = IndexOfLf(pending)) >= 0)
                {
                    var raw = pending.ToString(0, nl + 1);
                    pending.Remove(0, nl + 1);
                    _log.Add(ActivityKind.Rx, remote, raw);
                    var line = raw.TrimEnd('\n').TrimEnd('\r');
                    await session.HandleLineAsync(line, linked.Token);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException) { }
        catch (Exception ex)
        {
            _log.Add(ActivityKind.Error, remote, $"Error en la sesión: {ex.Message}");
        }
        finally
        {
            await session.DisposeAsync();
            channel.Close();
            lock (_gate) _clients.Remove(channel.Id);
            _log.Add(ActivityKind.Info, remote, "Cliente desconectado");
            ClientsChanged?.Invoke();
        }
    }

    private static int IndexOfLf(StringBuilder sb)
    {
        for (var i = 0; i < sb.Length; i++) if (sb[i] == '\n') return i;
        return -1;
    }

    private sealed class ClientChannel : IClientChannel
    {
        private readonly TcpClient _tcp;
        private readonly ActivityLog _log;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly CancellationTokenSource _closed = new();

        public ClientChannel(string id, string remote, TcpClient tcp, ActivityLog log)
        {
            Id = id; Remote = remote; _tcp = tcp; _log = log; Since = DateTime.Now;
        }

        public string Id { get; }
        public string Remote { get; }
        public DateTime Since { get; }
        public CancellationToken Token => _closed.Token;

        public async Task SendAsync(string text, bool continuous = false)
        {
            if (text.Length == 0) return;
            var bytes = Encoding.Latin1.GetBytes(text);
            await _writeLock.WaitAsync(_closed.Token);
            try
            {
                await _tcp.GetStream().WriteAsync(bytes, _closed.Token);
            }
            finally { _writeLock.Release(); }
            _log.Add(ActivityKind.Tx, Remote, text, continuous);
        }

        public void Close()
        {
            try { _closed.Cancel(); } catch { /* disposed */ }
            try { _tcp.Close(); } catch { /* already closed */ }
        }
    }
}
