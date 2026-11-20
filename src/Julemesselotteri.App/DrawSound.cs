using System.IO;
using System.Runtime.InteropServices;

namespace Julemesselotteri.App;

public sealed class DrawSound : IDisposable
{
    private const uint AsyncMemory = 0x0001 | 0x0002 | 0x0004;
    private const int SampleRate = 22050;
    private static readonly short[] Tick = MakeTick();
    private static readonly short[] PreviewTrack = MixTicks([0, 0.06, 0.14, 0.25, 0.42], 0.42);
    private GCHandle _playing;
    private short[]? _spinTrack;
    private int _spinVolume;

    [DllImport("winmm.dll", EntryPoint = "PlaySoundW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(nint data, nint module, uint flags);

    internal void Spin(short[] track, double elapsed, int volume, bool muted)
    {
        if (muted || volume <= 0) { Stop(); return; }
        if (_spinTrack == track && _spinVolume == volume) return;
        Play(CreateSpinWave(track, elapsed, volume));
        _spinTrack = track;
        _spinVolume = volume;
    }

    public void Preview(int volume, bool muted)
    {
        if (muted || volume <= 0) Stop();
        else Play(CreateSpinWave(PreviewTrack, 0, volume));
    }

    private void Play(byte[] wave)
    {
        Stop();
        _playing = GCHandle.Alloc(wave, GCHandleType.Pinned);
        if (!PlaySound(_playing.AddrOfPinnedObject(), 0, AsyncMemory))
            _playing.Free();
    }

    public void Stop()
    {
        _spinVolume = 0;
        _spinTrack = null;
        if (!_playing.IsAllocated) return;
        PlaySound(0, 0, 0);
        _playing.Free();
    }

    internal static short[] MixTicks(IEnumerable<double> crossings, double finish)
    {
        var track = new short[(int)Math.Round(finish * SampleRate) + Tick.Length];
        foreach (var time in crossings)
        {
            var start = (int)Math.Round(time * SampleRate);
            for (var i = 0; i < Tick.Length; i++)
                track[start + i] = (short)Math.Clamp(track[start + i] + Tick[i], short.MinValue, short.MaxValue);
        }
        return track;
    }

    private static short[] MakeTick()
    {
        var noise = new Random(720);
        var samples = new short[14 * SampleRate / 1000];
        var lastNoise = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            var t = (double)i / SampleRate;
            var grit = 2 * noise.NextDouble() - 1;
            var snap = (grit - lastNoise) * 0.48 * Math.Exp(-1100 * t);
            var ring = (0.57 * Math.Sin(2 * Math.PI * 2900 * t) + 0.23 * Math.Sin(2 * Math.PI * 4700 * t))
                * Math.Exp(-570 * t);
            lastNoise = grit;
            samples[i] = (short)(4200 * (snap + ring) * Math.Min(1, i / 3.0));
        }
        return samples;
    }

    internal static byte[] CreateSpinWave(short[] track, double elapsed, int volume)
    {
        var start = Math.Clamp((int)(elapsed * SampleRate), 0, track.Length);
        var dataSize = (track.Length - start) * 2;
        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataSize);
        var gain = Math.Clamp(volume, 0, 100) / 100.0;
        for (var i = start; i < track.Length; i++)
            writer.Write((short)(track[i] * gain));
        return stream.ToArray();
    }

    public void Dispose() => Stop();
}
