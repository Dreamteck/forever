using UnityEngine;

[CreateAssetMenu(menuName = "Forever/Mesh Batch Settings")]
public class MeshBatchSettings : ScriptableObject
{
    [System.Flags]
    public enum BatchEnvironment
    {
        None = 0,
        Desktop = 1 << 0,
        Console = 1 << 1,
        Mobile = 1 << 2,
        Web = 1 << 3,
        Other = 1 << 4,
        All = ~0
    }

    public bool excludeBatching
    {
        get { 
            if(_currentEnvironment == BatchEnvironment.None)
            {
                _currentEnvironment = GetBatchEnvironment();
            }
            return (_excludeBatching & _currentEnvironment) != 0; 
        }
    }

    [SerializeField]
    [Tooltip("Platforms to Exclude Batching On")]
    private BatchEnvironment _excludeBatching;

    private BatchEnvironment _currentEnvironment = BatchEnvironment.None;

    private static BatchEnvironment GetBatchEnvironment()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.LinuxEditor:
                return BatchEnvironment.Desktop;
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.LinuxPlayer:
                return BatchEnvironment.Desktop;
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.Android:
            case RuntimePlatform.WSAPlayerARM:
                return BatchEnvironment.Mobile;
            case RuntimePlatform.WebGLPlayer:
                return BatchEnvironment.Web;
            default:
                return BatchEnvironment.Other;
        }
    }
}
