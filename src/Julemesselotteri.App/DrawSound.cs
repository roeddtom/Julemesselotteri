using System.IO;
using System.Runtime.InteropServices;

namespace Julemesselotteri.App;

public sealed class DrawSound : IDisposable
{
    private GCHandle _playing;
    private readonly Dictionary<(int Volume, bool Win), byte[]> _cache = [];

    [DllImport("winmm.dll", EntryPoint = "PlaySoundW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(nint data, nint module, uint flags);

    public void Tick(int volume, bool muted) => Play(volume, muted, false);
    public void Win(int volume, bool muted) => Play(volume, muted, true);

    private void Play(int volume, bool muted, bool win)
    {
        if (muted || volume <= 0) { Stop(); return; }
        volume = Math.Clamp(volume, 0, 100);
        if (!_cache.TryGetValue((volume, win), out var wave))
        {
            wave = CreateWave(volume, win);
            _cache[(volume, win)] = wave;
        }
        Stop();
        // PlaySound reads asynchronously: keep the WAV pinned until playback is stopped.
        _playing = GCHandle.Alloc(wave, GCHandleType.Pinned);
        if (!PlaySound(_playing.AddrOfPinnedObject(), 0, 0x0001 | 0x0002 | 0x0004))
            Stop();
    }

    public void Stop()
    {
        if (!_playing.IsAllocated) return;
        PlaySound(0, 0, 0);
        _playing.Free();
    }

    public static byte[] CreateWave(int volume, bool win)
    {
        const int rate = 22050;
        var seconds = win ? 0.95 : 0.035;
        var samples = (int)(rate * seconds);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + samples * 2);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(rate);
        writer.Write(rate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(samples * 2);
        var amplitude = Math.Clamp(volume, 0, 100) / 100.0 * 0.22 * short.MaxValue;
        double[] notes = [523.25, 659.25, 783.99, 1046.5];
        for (var i = 0; i < samples; i++)
        {
            var t = (double)i / rate;
            double sample;
            if (win)
            {
                sample = 0;
                for (var note = 0; note < notes.Length; note++)
                {
                    var elapsed = t - note * 0.12;
                    if (elapsed < 0) continue;
                    sample += Math.Sin(2 * Math.PI * notes[note] * elapsed) *
                        Math.Min(1, elapsed / 0.008) * Math.Exp(-5 * elapsed) / 2;
                }
            }
            else
                sample = Math.Sin(2 * Math.PI * (950 - 500 * t / seconds) * t) *
                    Math.Sin(Math.PI * t / seconds) * Math.Exp(-80 * t);
            writer.Write((short)Math.Clamp(sample * amplitude, short.MinValue, short.MaxValue));
        }
        return stream.ToArray();
    }

    public void Dispose() => Stop();
}
