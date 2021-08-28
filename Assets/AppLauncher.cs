using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using LitJson;
using SA.iOS.UIKit;
// using Cameo.UI;
// using Cameo.cogame;

namespace Cameo
{
    public class AppLauncher : MonoBehaviour
    {
        [SerializeField]
        private string androidParamKey = "cameoAndroidParam";

        [SerializeField]
        private string iosLaunchedKey = "nmnsCollaborate";

        private string iosLaunchParamString;

        private JsonData iosLaunchParam;

        private Dictionary<string, JsonData> calledParams;

        /*

        使用方式：
        1. 使用 StartCoroutine(CheckCalledParamProcess()) 取得外部App傳入參數
        2. 使用 GetParam 從外部傳入參數取得需要的參數

        */

        public T GetParam<T>(string key) where T : class
        {
            if (calledParams.ContainsKey(key))
            {
                Debug.LogFormat("Ger param: {0}", key);
                return JsonMapper.ToObject<T>(calledParams[key].ToJson());
            }
            else
            {
                Debug.LogErrorFormat("Load launch param {0} error", key);
                return null;
            }
        }

        public IEnumerator CheckCalledParamProcess(string[] checkKeys)
        {
            calledParams = new Dictionary<string, JsonData>();

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                yield return iosParamChecker(checkKeys);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                androidParamChecker(checkKeys);
            }
        }

        #region Parse Called Params

        private void androidParamChecker(string[] checkKeys)
        {
            AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (currentActivity != null)
            {
                AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
                bool hasExtra = intent.Call<bool>("hasExtra", androidParamKey);
                AndroidJavaObject extras = intent.Call<AndroidJavaObject>("getExtras");

                if (hasExtra)
                {
                    foreach (string key in checkKeys)
                    {
                        try
                        {
                            JsonData data = JsonMapper.ToObject(extras.Call<string>("getString", key));
                            if (data != null)
                            {
                                calledParams.Add(key, data);
                            }
                            else
                            {
                                Debug.LogErrorFormat("Android: Get launce data {0} error!", key);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogErrorFormat("Android: get extra call error! key: {0}", key);
                        }
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Find Extra {0} error!", androidParamKey);
                }
            }
        }

        public void setIOSCalledParam(string iosLaunchParamString)
        {
            this.iosLaunchParamString = UnityWebRequest.UnEscapeURL(iosLaunchParamString);

            if (!string.IsNullOrEmpty(iosLaunchParamString))
            {
                string validString = iosLaunchParamString.Remove(0, (iosLaunchedKey + "://").Length); //iosLaunchKey + "://"
                string jsonStr = UnityWebRequest.UnEscapeURL(validString);
                Debug.LogFormat("Parse iOS launch param: {0}", jsonStr);

                iosLaunchParam = JsonMapper.ToObject(jsonStr);
            }

            Debug.LogFormat("Get iOS launch param: {0}, After unescape:{1}", iosLaunchParam, this.iosLaunchParamString);
        }

        private IEnumerator iosParamChecker(string[] checkKeys)
        {
            float waitTime = 3;

            while (waitTime > 0 && string.IsNullOrEmpty(iosLaunchParamString))
            {
                yield return new WaitForSeconds(0.05f);
                waitTime -= 0.05f;
            }

            if (string.IsNullOrEmpty(iosLaunchParamString))
            {
                Debug.LogError("Load iOS launch param failed, launch param is empty");
            }
            else
            {
                foreach (string key in checkKeys)
                {
                    if (!iosLaunchParam.Keys.Contains(key))
                    {
                        Debug.LogErrorFormat("Check key: {0} is empty", key);
                    }
                    else
                    {
                        Debug.LogFormat("Add ({0}, {1})", key, iosLaunchParam[key].ToJson());
                        calledParams.Add(key, iosLaunchParam[key]);
                    }
                }
            }
        }
        #endregion

        public void Launch(string launchTarget, Dictionary<string, object> lauchParam)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                LaunchIOS(launchTarget, lauchParam);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                LaunchAndroid(launchTarget, lauchParam);
            }
            Debug.LogFormat("Launch app with param: {0}", JsonMapper.ToJson(lauchParam));
        }

        #region Launchers

        public void LaunchAndroid(string target, Dictionary<string, object> launchParam, string downloadUrl = null)
        {
            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", target);

            if (launchIntent != null)
            {
                foreach (string paramKey in launchParam.Keys)
                {
                    Debug.Log("Put extra: (" + paramKey + ", " + launchParam[paramKey] + ")");
                    launchIntent.Call<AndroidJavaObject>("putExtra", paramKey, launchParam[paramKey]);
                }
                ca.Call("startActivity", launchIntent);
            }
            else
            {
                openDownloadPage(downloadUrl);
            }
        }

        public void LaunchAndroid(string target, string paramKey, string paramValue, string downloadUrl = null)
        {
            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", target);

            if (launchIntent != null)
            {
                launchIntent.Call<AndroidJavaObject>("putExtra", paramKey, paramValue);
                ca.Call("startActivity", launchIntent);
            }
            else
            {
                openDownloadPage(downloadUrl);
            }
        }

        public void LaunchIOS(string target, Dictionary<string, object> launchParam, string downloadUrl = null)
        {
            string jsonParam = JsonMapper.ToJson(launchParam);
            string launchParamStr = UnityWebRequest.EscapeURL(jsonParam);
            string url = string.Format("{0}://{1}", target, launchParamStr);

            LaunchIOS(url, downloadUrl);
        }

        public void LaunchIOS(string url, string downloadUrl)
        {
            if (ISN_UIApplication.CanOpenURL(url))
            {
                ISN_UIApplication.OpenURL(url);
            }
            else
            {
                openDownloadPage(downloadUrl);
            }
        }

        private void openDownloadPage(string downloadUrl)
        {
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                Application.OpenURL(downloadUrl);
            }
            else
            { 
                Dictionary<string, object> paramMaps = new Dictionary<string, object>();
                // paramMaps.Add(SimpleMessageBox.MESSAGE_KEY, "尚未安裝所需的App");
                // MessageBoxManager.Instance.ShowMessageBox("Simple", paramMaps);
            }
        }

        #endregion
    }
}
