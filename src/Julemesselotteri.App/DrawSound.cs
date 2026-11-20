using System.IO;
using System.Runtime.InteropServices;

namespace Julemesselotteri.App;

public sealed class DrawSound : IDisposable
{
    private const uint AsyncMemory = 0x0001 | 0x0002 | 0x0004;
    private const uint Loop = 0x0008;
    private static readonly Lazy<byte[]> SpinWave = new(() => LoadWave("slot-spin"));
    private static readonly Lazy<byte[]> WinWave = new(() => LoadWave("slot-win"));
    private GCHandle _playing;
    private int _spinVolume;

    [DllImport("winmm.dll", EntryPoint = "PlaySoundW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(nint data, nint module, uint flags);

    public void Spin(int volume, bool muted)
    {
        if (muted || volume <= 0) { Stop(); return; }
        if (_spinVolume == volume) return;
        Play(volume, false);
        _spinVolume = volume;
    }

    public void Win(int volume, bool muted)
    {
        if (muted || volume <= 0) Stop();
        else Play(volume, true);
    }

    private void Play(int volume, bool win)
    {
        Stop();
        _playing = GCHandle.Alloc(CreateWave(volume, win), GCHandleType.Pinned);
        if (!PlaySound(_playing.AddrOfPinnedObject(), 0, AsyncMemory | (win ? 0u : Loop)))
            _playing.Free();
    }

    public void Stop()
    {
        _spinVolume = 0;
        if (!_playing.IsAllocated) return;
        PlaySound(0, 0, 0);
        _playing.Free();
    }

    public static byte[] CreateWave(int volume, bool win)
    {
        var wave = (byte[])(win ? WinWave.Value : SpinWave.Value).Clone();
        var gain = Math.Clamp(volume, 0, 100) / 100.0;
        // The bundled clips are mono 16-bit PCM WAVs with a canonical 44-byte header.
        for (var i = 44; i < wave.Length; i += 2)
        {
            var sample = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(wave.AsSpan(i));
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(wave.AsSpan(i), (short)(sample * gain));
        }
        return wave;
    }

    private static byte[] LoadWave(string name)
    {
        using var resource = typeof(DrawSound).Assembly.GetManifestResourceStream($"{name}.wav")
            ?? throw new InvalidOperationException($"Missing bundled audio: {name}.wav");
        using var stream = new MemoryStream();
        resource.CopyTo(stream);
        return stream.ToArray();
    }

    public void Dispose() => Stop();
}
