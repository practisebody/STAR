using LCY;
using System;
using System.Collections.Generic;
using System.Text;

namespace LCY
{
    public sealed class Configurations : Singleton<Configurations>
    {
        private volatile Dictionary<string, object> Configs = new Dictionary<string, object>();
        public delegate void Handler();
        public delegate void OnChangeHandler(dynamic v);
        private Dictionary<string, Delegate> Events = new Dictionary<string, Delegate>();

        #region configs

        public bool Contains(string key)
        {
            return Configs.ContainsKey(key);
        }

        private void Set(string key, object value, Type T)
        {
            if (T == null)
                Configs[key] = null;
            else
                Configs[key] = Convert.ChangeType(value, T);
            Delegate del;
            if (Events.TryGetValue(key, out del))
                del.DynamicInvoke(Get(key));
        }

        public void Set(string key, object value)
        {
            if (Contains(key))
                Set(key, value, Configs[key]?.GetType());
            else
                Set(key, value, value?.GetType());
        }

        public void SetType(string key, Type T)
        {
            Configs[key] = Convert.ChangeType(Configs[key], T);
        }

        public void Unset(string key)
        {
            Configs.Remove(key);
        }

        public dynamic Get(string key)
        {
            return Configs[key];
        }

        public T Get<T>(string key)
        {
            return (T)Convert.ChangeType(Configs[key], typeof(T));
        }

        public dynamic Get(string key, dynamic def)
        {
            if (Contains(key))
                return Get(key);
            else
                return def;
        }

        public dynamic this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        public void Clear()
        {
            Configs.Clear();
        }

        #endregion

        #region callbacks

        public enum CallNow
        {
            YES,
            NO,
        };

        public enum RunOnMainThead
        {
            YES,
            NO,
        };

        public enum WaitUntilDone
        {
            YES,
            NO,
        };

        public void SetAndAddCallback(string key, object value, OnChangeHandler callback, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            SetAndAddCallback(key, value, callback, CallNow.NO, runOnMainThread, waitUntilDone);
        }

        public void SetAndAddCallback(string key, object value, OnChangeHandler callback, CallNow callNow, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            Set(key, value);
            AddCallback(key, callback, runOnMainThread, waitUntilDone);
            if (callNow == CallNow.YES)
            {
                Invoke(key);
            }
        }

        public void AddCallback(string key, Handler callback, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            if (Contains(key) == false)
                Configs[key] = null;
            Delegate del = runOnMainThread == RunOnMainThead.YES ? () => Utilities.InvokeMain(() => callback(), waitUntilDone == WaitUntilDone.YES ? true : false) : callback;
            if (Events.ContainsKey(key))
            {
                Events[key] = Delegate.Combine(Events[key], del);
            }
            else
            {
                Events[key] = del;
            }
        }

        public void AddCallback(string key, OnChangeHandler callback, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            if (Contains(key) == false)
                Configs[key] = null;
            Delegate del = runOnMainThread == RunOnMainThead.YES ? (dynamic v) => Utilities.InvokeMain(() => callback(v), waitUntilDone == WaitUntilDone.YES ? true : false) : callback;
            if (Events.ContainsKey(key))
            {
                Events[key] = Delegate.Combine(Events[key], del);
            }
            else
            {
                Events[key] = del;
            }
        }

        public void RemoveCallback(string key, OnChangeHandler callback)
        {
            if (Events.ContainsKey(key))
            {
                Events[key] = Delegate.Remove(Events[key], callback);
            }
        }

        public void RemoveAllCallback(string key)
        {
            if (Events.ContainsKey(key))
            {
                Events.Remove(key);
            }
        }

        public void RemoveAllCallback()
        {
            Events.Clear();
        }

        public void TryInvoke(string key)
        {
            if (Contains(key))
            {
                Delegate del;
                if (Events.TryGetValue(key, out del))
                {
                    del.DynamicInvoke(Get(key));
                }
            }
        }

        public void Invoke(string key)
        {
            Events[key].DynamicInvoke(Get(key));
        }

        #endregion

        #region utilites

        override public string ToString()
        {
            return ToString(":");
        }

        public string ToString(string sep, string endl = "")
        {
            Dictionary<string, object> temp = Configs;
            StringBuilder sb = new StringBuilder(temp.Count * 30);
            foreach (KeyValuePair<string, object> c in temp)
                sb.Append(c.Key).Append(sep).Append(c.Value?.ToString()).Append(endl);
            return sb.ToString();
        }

        #endregion
    }
}