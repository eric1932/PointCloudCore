using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// ref: https://developer.aliyun.com/article/661664
// another not used: https://blog.csdn.net/wannaconquer/article/details/125313087
public class AndroidAudio : MonoBehaviour
{
    private const string currentVolume = "getStreamVolume";         //当前音量
    private const string maxVolume = "getStreamMaxVolume";          //最大音量

    private const int STREAM_VOICE_CALL = 0;                        // 通话音量
    private const int STREAM_SYSTEM = 1;                            // 系统音量
    private const int STREAM_RING = 2;                              // 铃声音量
    private const int STREAM_MUSIC = 3;                             // 媒体音量
    private const int STREAM_ALARM = 4;                             // 警报音量 
    private const int STREAM_NOTIFICATION = 5;                      // 窗口顶部状态栏 Notification
    private const int STREAM_DTMF = 8;                              // 双音多频
    private const int ADJUST_LOWER = 9;                             // 双音多频


    private static AndroidJavaObject audioManager;


    //创建几个 "3D Text" 用于接收音量值
    public string STREAM_VOICE_CALL_Text = "0";
    public string STREAM_SYSTEM_Text = "0";
    public string STREAM_RING_Text = "0";
    public string STREAM_MUSIC_Text = "0";
    public string STREAM_ALARM_Text = "0";
    public string STREAM_NOTIFICATION_Text = "0";
    public string STREAM_DTMF_Text = "0";

    public string MaxSTREAM_VOICE_CALL_Text = "0";
    public string MaxSTREAM_SYSTEM_Text = "0";
    public string MaxSTREAM_RING_Text = "0";
    public string MaxSTREAM_MUSIC_Text = "0";
    public string MaxSTREAM_ALARM_Text = "0";
    public string MaxSTREAM_NOTIFICATION_Text = "0";
    public string MaxSTREAM_DTMF_Text = "0";


#if UNITY_ANDROID && !UNITY_EDITOR
    void Awake()
    {
        AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        audioManager = currentActivity.Call<AndroidJavaObject>("getSystemService", new AndroidJavaObject("java.lang.String", "audio"));

        MaxSTREAM_VOICE_CALL_Text = audioManager.Call<int>(maxVolume, STREAM_VOICE_CALL).ToString();
        MaxSTREAM_SYSTEM_Text = audioManager.Call<int>(maxVolume, STREAM_SYSTEM).ToString();
        MaxSTREAM_RING_Text = audioManager.Call<int>(maxVolume, STREAM_RING).ToString();
        MaxSTREAM_MUSIC_Text = audioManager.Call<int>(maxVolume, STREAM_MUSIC).ToString();
        MaxSTREAM_ALARM_Text = audioManager.Call<int>(maxVolume, STREAM_ALARM).ToString();
        MaxSTREAM_NOTIFICATION_Text = audioManager.Call<int>(maxVolume, STREAM_NOTIFICATION).ToString();
        MaxSTREAM_DTMF_Text = audioManager.Call<int>(maxVolume, STREAM_DTMF).ToString();
    }

    void Update()
    {
        STREAM_VOICE_CALL_Text = audioManager.Call<int>(currentVolume, STREAM_VOICE_CALL).ToString();
        STREAM_SYSTEM_Text = audioManager.Call<int>(currentVolume, STREAM_SYSTEM).ToString();
        STREAM_RING_Text = audioManager.Call<int>(currentVolume, STREAM_RING).ToString();
        STREAM_MUSIC_Text = audioManager.Call<int>(currentVolume, STREAM_MUSIC).ToString();
        STREAM_ALARM_Text = audioManager.Call<int>(currentVolume, STREAM_ALARM).ToString();
        STREAM_NOTIFICATION_Text = audioManager.Call<int>(currentVolume, STREAM_NOTIFICATION).ToString();
        STREAM_DTMF_Text = audioManager.Call<int>(currentVolume, STREAM_DTMF).ToString();
    }
#endif

}
