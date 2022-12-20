using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using CSCore.CoreAudioAPI;
using System.Diagnostics;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Text.Json;

/*
namespace SetAppVolumne
{
    class Program
    {
        static void Main(string[] args)
        {
            const string app = "Mozilla Firefox";

            IEnumerable<string> appNames = EnumerateApplications();
            foreach (string name in appNames)
            {
                Console.WriteLine("name:" + name);
                if (name == app)
                {
                    // display mute state & volume level (% of master)
                    Console.WriteLine("Mute:" + GetApplicationMute(app));
                    Console.WriteLine("Volume:" + GetApplicationVolume(app));

                    // mute the application
                    //SetApplicationMute(app, true);

                    // set the volume to half of master volume (50%)
                    //SetApplicationVolume(app, 50);
                }
            }
            Console.ReadKey();
        }

        public static float? GetApplicationVolume(string name)
        {
            ISimpleAudioVolume volume = GetVolumeObject(name);
            if (volume == null)
                return null;

            float level;
            volume.GetMasterVolume(out level);
            return level * 100;
        }

        public static bool? GetApplicationMute(string name)
        {
            ISimpleAudioVolume volume = GetVolumeObject(name);
            if (volume == null)
                return null;

            bool mute;
            volume.GetMute(out mute);
            return mute;
        }

        public static void SetApplicationVolume(string name, float level)
        {
            ISimpleAudioVolume volume = GetVolumeObject(name);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMasterVolume(level / 100, ref guid);
        }

        public static void SetApplicationMute(string name, bool mute)
        {
            ISimpleAudioVolume volume = GetVolumeObject(name);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMute(mute, ref guid);
        }

        public static IEnumerable<string> EnumerateApplications()
        {
            // get the speakers (1st render + multimedia) device
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            // activate the session manager. we need the enumerator
            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl ctl;
                sessionEnumerator.GetSession(i, out ctl);
                string dn;
                ctl.GetDisplayName(out dn);
                yield return dn;
                Marshal.ReleaseComObject(ctl);
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
        }

        private static ISimpleAudioVolume GetVolumeObject(string name)
        {
            // get the speakers (1st render + multimedia) device
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            // activate the session manager. we need the enumerator
            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            // search for an audio session with the required name
            // NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
            ISimpleAudioVolume volumeControl = null;
            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl ctl;
                sessionEnumerator.GetSession(i, out ctl);
                string dn;
                ctl.GetDisplayName(out dn);
                if (string.Compare(name, dn, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    volumeControl = ctl as ISimpleAudioVolume;
                    break;
                }
                Marshal.ReleaseComObject(ctl);
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
            return volumeControl;
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int NotImpl1();

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

        // the rest is not implemented
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        // the rest is not implemented
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int NotImpl1();
        int NotImpl2();

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        // the rest is not implemented
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl Session);
    }

    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl
    {
        int NotImpl1();

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        // the rest is not implemented
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float fLevel, ref Guid EventContext);

        [PreserveSig]
        int GetMasterVolume(out float pfLevel);

        [PreserveSig]
        int SetMute(bool bMute, ref Guid EventContext);

        [PreserveSig]
        int GetMute(out bool pbMute);
    }
}*/

[System.Serializable]
public class UpdateSliderValueMessage
{
    public int channel { get; set; }
    public float volume { get; set; }
}
class AudioSessionEvents: IAudioSessionEvents
{
    public AudioSessionEvents(int ProcessId, float Volume, int Index)
    {
        this.ProcessID = ProcessId;
        this.Volume = Volume;
        this.Index = Index;

        using (var p = Process.GetProcessById(ProcessID))
        {
            this.ProcessName = p.ProcessName;
            Icon icon = p.GetIcon();
            if (icon != null)
            {
                icon.ToBitmap().Save("C:/Users/Jan/Desktop/icons/" + ProcessName + ".bmp", ImageFormat.Bmp);
            }
        }

        Console.WriteLine(ProcessName + ": " + Volume);
    }
    public void OnDisplayNameChanged(string newDisplayName, ref Guid eventContext)
    { }

    public void OnIconPathChanged(string newIconPath, ref Guid eventContext)
    { }

    public void OnSimpleVolumeChanged(float newVolume, bool newMute, ref Guid eventContext)
    {

        Volume = newVolume;

        
    }

    public void OnChannelVolumeChanged(int channelCount, float[] newChannelVolumeArray, int changedChannel, ref Guid eventContext)
    { }

    public void OnGroupingParamChanged(ref Guid newGroupingParam, ref Guid eventContext)
    { }

    public void OnStateChanged(AudioSessionState newState)
    {}

    public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
    {}

    public void SetCurrentIndex(int index)
    {
        Index = index;
    }

    public void Check()
    {
        UpdateSliderValueMessage msg = new UpdateSliderValueMessage() { channel = Index, volume = Volume };
        String UpdateText = JsonSerializer.Serialize<UpdateSliderValueMessage>(msg);
        //Console.WriteLine(ProcessName + ": " + Volume);
        Console.WriteLine(UpdateText);
        Program.port.WriteLine(UpdateText);
        String response = Program.port.ReadLine();
        Console.WriteLine(response);
    }

    private int ProcessID;
    private float Volume;

    private String ProcessName;
    private int Index;

}
class Program
{
    private static List<AudioSessionEvents> Events = new List<AudioSessionEvents>();
    public static SerialPort port = new SerialPort();
    private static List<AudioSessionControl2> SessionControls = new List<AudioSessionControl2>();
    public static void Main(string[] args)
    {
        port.PortName = "COM3";
        port.BaudRate = 9600;
        port.Open();

        AudioSessionManager2 sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
        AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
        foreach (AudioSessionControl session in sessionEnumerator)
        {
            SimpleAudioVolume simpleVolume = session.QueryInterface<SimpleAudioVolume>();
            AudioSessionControl2 sessionControl = session.QueryInterface<AudioSessionControl2>();
            if (sessionControl.IsSystemSoundSession)
            {
                continue;
            }
            SessionControls.Add(sessionControl);
            float currentVolume;
            simpleVolume.GetMasterVolumeNative(out currentVolume);
            Events.Add(new AudioSessionEvents(sessionControl.ProcessID, currentVolume, Events.Count()));
            sessionControl.RegisterAudioSessionNotification(Events.Last());
        }


        while (true)
        {
            Thread.Sleep(100);

            foreach(AudioSessionEvents events in Events)
            {
                events.Check();
            }
        }

    }


    private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
    {
        using (var enumerator = new MMDeviceEnumerator())
        {
            using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
            {
                //Console.WriteLine("DefaultDevice: " + device.FriendlyName);
                var sessionManager = AudioSessionManager2.FromMMDevice(device);
                return sessionManager;
            }
        }
    }
}

public static class ProcessExtensions
{
    [DllImport("Kernel32.dll")]
    private static extern uint QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

    public static string GetMainModuleFileName(this Process process, int buffer = 1024)
    {
        var fileNameBuilder = new StringBuilder(buffer);
        uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
        return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) != 0 ?
            fileNameBuilder.ToString() :
            null;
    }

    public static Icon GetIcon(this Process process)
    {
        try
        {
            string mainModuleFileName = process.GetMainModuleFileName();
            return Icon.ExtractAssociatedIcon(mainModuleFileName);
        }
        catch
        {
            // Probably no access
            return null;
        }
    }
}

