using LCY;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LCY
{
    public sealed class Configurations : Singleton<Configurations>
    {
        protected volatile Dictionary<string, object> Configs = new Dictionary<string, object>();
        public delegate void OnChangeCallback(dynamic v);
        protected Dictionary<string, Delegate> Events = new Dictionary<string, Delegate>();

        #region configs

        public bool Contains(string key)
        {
            return Configs.ContainsKey(key);
        }

        protected void Set(string key, object value, Type T)
        {
            Configs[key] = Convert.ChangeType(value, T);
            Delegate del;
            if (Events.TryGetValue(key, out del))
                del.DynamicInvoke(Get(key));
        }

        public void Set(string key, object value)
        {
            if (Contains(key))
                Set(key, value, Configs[key].GetType());
            else
                Set(key, value, value.GetType());
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

        public void SetAndAddCallback(string key, object value, OnChangeCallback callback, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            SetAndAddCallback(key, value, callback, CallNow.NO, runOnMainThread, waitUntilDone);
        }

        public void SetAndAddCallback(string key, object value, OnChangeCallback callback, CallNow callNow, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            if (callNow == CallNow.YES)
            {
                AddCallback(key, callback, runOnMainThread, waitUntilDone);
                Set(key, value);
            }
            else
            {
                Set(key, value);
                AddCallback(key, callback, runOnMainThread, waitUntilDone);
            }
        }

        public void AddCallback(string key, OnChangeCallback callback, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            Delegate del = runOnMainThread == RunOnMainThead.YES ? (OnChangeCallback)((dynamic v) => Utilities.InvokeMain(() => callback(v), waitUntilDone == WaitUntilDone.YES ? true : false)) : callback;
            if (Events.ContainsKey(key))
            {
                Events[key] = Delegate.Combine(Events[key], del);
            }
            else
            {
                Events[key] = del;
            }
        }

        public void RemoveCallback(string key, OnChangeCallback callback)
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
                    del.DynamicInvoke(key);
                }
            }
        }

        public void Invoke(string key)
        {
            Events[key].DynamicInvoke(Configs[key]);
        }

        #endregion

        #region utilites

        override public string ToString()
        {
            return ToString(":");
        }

        public string ToString(string sep, string endl = "")
        {
            string result = "";
            Dictionary<string, object> temp = Configs;
            foreach (KeyValuePair<string, object> c in temp)
                result += c.Key + sep + c.Value.ToString() + endl;
            return result;
        }

        #endregion
    }
}