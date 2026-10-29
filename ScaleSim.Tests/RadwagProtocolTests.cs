using System.Net;
using System.Net.Sockets;
using System.Text;
using ScaleSim.Core;
using ScaleSim.Core.Abstractions;
using ScaleSim.Core.Networking;
using ScaleSim.Radwag;

namespace ScaleSim.Tests;

public class RadwagProtocolTests
{
    private sealed class FakeChannel : IClientChannel
    {
        private readonly List<string> _sent = new();
        public string Id => "t";
        public string Remote => "test";
        public Task SendAsync(string text, bool continuous = false)
        {
            lock (_sent) _sent.Add(text);
            return Task.CompletedTask;
        }
        public string All() { lock (_sent) return string.Concat(_sent); }
    }

    private static (RadwagSession session, FakeChannel channel, ScaleContext ctx) Create(Action<ScaleOptions>? tweak = null)
    {
        var model = new RadwagModel();
        var options = model.CreateDefaultOptions();
        options.StabilizationMs = 0; options.TareMs = 0; options.ZeroMs = 0;
        tweak?.Invoke(options);
        var ctx = new ScaleContext(new ScaleState(options), options, new ActivityLog(), _ => { });
        var channel = new FakeChannel();
        var session = (RadwagSession)new RadwagProtocol(ctx, model.Modes).OpenSession(channel);
        return (session, channel, ctx);
    }

    private static async Task<string> Run(RadwagSession s, FakeChannel c, string line)
    {
        await s.HandleLineAsync(line, CancellationToken.None);
        return c.All();
    }

    [Fact]
    public async Task S_matches_the_esp32_sketch_frame()
    {
        var (s, c, ctx) = Create();
        ctx.State.SetLoad(0, 12.5);
        // Sketch: "S A\r\n" then sprintf("S    +%9.2f_kg*\r\n")
        Assert.Equal("S_A\r\nS    +    12.50 kg*\r\n", await Run(s, c, "S"));
    }

    [Fact]
    public async Task OT_matches_the_esp32_sketch_frame()
    {
        var (s, c, ctx) = Create();
        ctx.State.SetLoad(0, 12.5);
        ctx.State.Tare(0);
        Assert.Equal("OT     12.50 kg* \r\n", await Run(s, c, "OT"));
    }

    [Fact]
    public async Task Tare_makes_net_zero_and_zero_command_works()
    {
        var (s, c, ctx) = Create();
        ctx.State.SetLoad(0, 5);
        Assert.Equal("T_A\r\nT_D\r\n", await Run(s, c, "T"));
        Assert.Contains("S    +     0.00 kg*", await Run(s, c, "S"));
        ctx.State.SetLoad(0, 0);
        Assert.Contains("S    -     5.00 kg*", await Run(s, c, "S"));
    }

    [Fact]
    public async Task Zero_out_of_range_answers_caret()
    {
        var (s, c, ctx) = Create();
        ctx.State.SetLoad(0, 500);
        Assert.Equal("Z_A\r\nZ_^\r\n", await Run(s, c, "Z"));
    }

    [Fact]
    public async Task Tare_with_nothing_on_the_pan_answers_v()
    {
        var (s, c, _) = Create();
        Assert.Equal("T_A\r\nT_v\r\n", await Run(s, c, "T"));
    }

    [Fact]
    public async Task Unstable_scale_times_out_on_S_but_answers_SI_with_question_mark()
    {
        var (s, c, ctx) = Create(o => o.StabilityTimeoutMs = 120);
        ctx.State.SetLoad(0, 3);
        ctx.State.Stable = false;
        Assert.Equal("S_A\r\nS_E\r\n", await Run(s, c, "S"));
        Assert.Matches(@"SI \? \+ +[0-9.]+ kg\*\r\n$", await Run(s, c, "SI"));
    }

    [Fact]
    public async Task Manual_format_options_use_space_sign_and_plain_unit()
    {
        var (s, c, ctx) = Create(o =>
        {
            o.Extra[RadwagModel.PositiveSignKey] = "espacio";
            o.Extra[RadwagModel.UnitMarkerKey] = "false";
        });
        ctx.State.SetLoad(0, 8.5);
        Assert.Equal("SI    " + "     8.50 kg \r\n", await Run(s, c, "SI"));
    }

    [Fact]
    public async Task SU_uses_the_current_unit()
    {
        var (s, c, ctx) = Create();
        ctx.State.SetLoad(0, 1.5);
        ctx.State.Unit = "g";
        Assert.Equal("SUI  +     1500 g *\r\n", await Run(s, c, "SUI"));
    }

    [Fact]
    public async Task Overload_flags_the_stability_column()
    {
        var (s, c, ctx) = Create();
        ctx.State.SetLoad(0, 1600);
        Assert.StartsWith("SI ^ +", await Run(s, c, "SI"));
    }

    [Fact]
    public async Task SIA_lists_every_platform()
    {
        var (s, c, ctx) = Create(o => o.Platforms = 2);
        ctx.State.OnOptionsChanged("kg");
        ctx.State.SetLoad(0, 118.5);
        ctx.State.SetLoad(1, 36.2);
        Assert.Equal("P1   +   118.50 kg*\r\nP2   +    36.20 kg*\r\n", await Run(s, c, "SIA"));
    }

    [Theory]
    [InlineData("NB", "NB_A_\"123456\"\r\n")]
    [InlineData("UT 2.5", "UT_OK\r\n")]
    [InlineData("UT abc", "ES\r\n")]
    [InlineData("UT 2,5", "ES\r\n")]
    [InlineData("UT", "ES\r\n")]
    [InlineData("DH 1.5", "DH_OK\r\n")]
    [InlineData("BP 350", "BP_OK\r\n")]
    [InlineData("BP x", "ES\r\n")]
    [InlineData("P1", "P1_OK\r\n")]
    [InlineData("P3", "ES\r\n")]
    [InlineData("OMS 2", "OMS_OK\r\n")]
    [InlineData("OMS 99", "OMS_E\r\n")]
    [InlineData("OMG", "OMG_1_Pesaje\r\n")]
    [InlineData("SM 1.2", "SM_I\r\n")]
    [InlineData("RM 1.2", "RM_I\r\n")]
    [InlineData("C0", "C0_A\r\n")]
    [InlineData("HOLA", "ES\r\n")]
    [InlineData("S 5", "ES\r\n")]
    public async Task Simple_commands(string command, string expected)
    {
        var (s, c, _) = Create();
        Assert.Equal(expected, await Run(s, c, command));
    }

    [Fact]
    public async Task Piece_count_mode_enables_SM()
    {
        var (s, c, _) = Create();
        await s.HandleLineAsync("OMS 2", CancellationToken.None);
        Assert.EndsWith("SM_OK\r\n", await Run(s, c, "SM 0.25"));
    }

    [Fact]
    public async Task OMI_lists_modes_then_OK()
    {
        var (s, c, _) = Create();
        var text = await Run(s, c, "OMI");
        Assert.StartsWith("OMI\r\n1_Pesaje\r\n2_Calculo de piezas\r\n3_Desviaciones\r\n", text);
        Assert.EndsWith("OK\r\n", text);
    }

    [Fact]
    public async Task Threshold_round_trip()
    {
        var (s, c, _) = Create();
        await s.HandleLineAsync("UH 20.5", CancellationToken.None);
        Assert.EndsWith("UH     20.50 kg* \r\n", await Run(s, c, "OUH"));
    }

    [Fact]
    public async Task Unavailable_scale_answers_I()
    {
        var (s, c, _) = Create(o => o.Unavailable = true);
        Assert.Equal("S_I\r\n", await Run(s, c, "S"));
    }

    [Fact]
    public async Task Commands_are_case_insensitive_and_tolerate_trailing_blanks()
    {
        var (s, c, _) = Create();
        Assert.Equal("NB_A_\"123456\"\r\n", await Run(s, c, "nb  "));
    }

    [Fact]
    public async Task PC_lists_commands()
    {
        var (s, c, _) = Create();
        Assert.StartsWith("PC_A_\"Z,T,S,SI", await Run(s, c, "PC"));
    }

    [Fact]
    public async Task Continuous_mode_streams_until_C0()
    {
        var (s, c, ctx) = Create(o => o.ContinuousIntervalMs = 30);
        ctx.State.SetLoad(0, 2);
        await s.HandleLineAsync("C1", CancellationToken.None);
        await Task.Delay(250);
        await s.HandleLineAsync("C0", CancellationToken.None);
        var text = c.All();
        Assert.StartsWith("C1_A\r\n", text);
        Assert.True(text.Split("SI ").Length - 1 >= 3, text);
        // Every continuous frame is its own CRLF-terminated line.
        Assert.All(text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Where(l => l.StartsWith("SI")),
            l => Assert.Matches(@"^SI   \+ +2\.00 kg\*$", l));
        await Task.Delay(100);
        var afterStop = c.All();
        await Task.Delay(150);
        Assert.Equal(afterStop, c.All());
    }

    [Fact]
    public async Task Real_tcp_client_gets_sketch_style_conversation()
    {
        var model = new RadwagModel();
        var options = model.CreateDefaultOptions();
        options.StabilizationMs = 0; options.TareMs = 10;
        var ctx = new ScaleContext(new ScaleState(options), options, new ActivityLog(), _ => { });
        ctx.State.SetLoad(0, 7.25);

        var port = FreePort();
        var server = new TcpScaleServer(model.CreateProtocol(ctx), ctx.Log);
        Assert.Null(server.Start(port));
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(IPAddress.Loopback, port);
            var stream = tcp.GetStream();
            // Two commands in one packet must both be answered, in order.
            await stream.WriteAsync(Encoding.ASCII.GetBytes("T\r\nS\r\n"));
            var expected = "T_A\r\nT_D\r\nS_A\r\nS    +     0.00 kg*\r\n";
            Assert.Equal(expected, await ReadExactly(stream, expected.Length));
            Assert.Single(server.Clients);
        }
        finally { server.Stop(); }
    }

    [Fact]
    public void Starting_on_a_busy_port_reports_an_error()
    {
        var port = FreePort();
        var model = new RadwagModel();
        var options = model.CreateDefaultOptions();
        var ctx = new ScaleContext(new ScaleState(options), options, new ActivityLog(), _ => { });
        var blocker = new TcpListener(IPAddress.Any, port);
        blocker.Start();
        try
        {
            var server = new TcpScaleServer(model.CreateProtocol(ctx), ctx.Log);
            Assert.Contains("ya está en uso", server.Start(port));
        }
        finally { blocker.Stop(); }
    }

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static async Task<string> ReadExactly(NetworkStream stream, int count)
    {
        var buffer = new byte[count];
        var got = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (got < count)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(got), cts.Token);
            if (n == 0) break;
            got += n;
        }
        return Encoding.ASCII.GetString(buffer, 0, got);
    }
}
