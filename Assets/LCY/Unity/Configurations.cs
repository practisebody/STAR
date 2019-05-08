using LCY;
using System;
using System.Collections.Generic;
using System.Text;

namespace LCY
{
    /// <summary>
    /// A configuration class, singleton, only one copy, access by Instance.
    /// A Dictionary of key-value pairs, each associated with callback function(s).
    /// Whenever a value changes, the corresponding callback function(s) will be called
    /// Keys are strings, while values can be any type
    /// Callback function(s) can be zero or one parameter functions, can each key can
    /// associate with more than one callback function
    /// </summary>
    public sealed class Configurations : Singleton<Configurations>
    {
        private volatile Dictionary<string, object> Configs = new Dictionary<string, object>();
        public delegate void Handler();
        public delegate void OnChangeHandler(dynamic v);
        private Dictionary<string, Delegate> Events = new Dictionary<string, Delegate>();

        #region configs

        /// <summary>
        /// Returns true if contains a specific key
        /// </summary>
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

        /// <summary>
        /// Sets the value corresponds to a key
        /// </summary>
        public void Set(string key, object value)
        {
            if (Contains(key))
                Set(key, value, Configs[key]?.GetType());
            else
                Set(key, value, value?.GetType());
        }

        /// <summary>
        /// Sets the type of value corresponds to a key
        /// </summary>
        public void SetType(string key, Type T)
        {
            Configs[key] = Convert.ChangeType(Configs[key], T);
        }

        /// <summary>
        /// Unset a key, remove its value and callbacks
        /// </summary>
        public void Unset(string key)
        {
            Configs.Remove(key);
        }

        /// <summary>
        /// Returns the value corresponds to a key
        /// </summary>
        public dynamic Get(string key)
        {
            return Configs[key];
        }

        /// <summary>
        /// Returns the value corresponds to a key, converts to type T
        /// </summary>
        public T Get<T>(string key)
        {
            return (T)Convert.ChangeType(Configs[key], typeof(T));
        }

        /// <summary>
        /// Returns the value corresponds to a key if available
        /// Otherwise return the default value
        /// </summary>
        public dynamic Get(string key, dynamic def)
        {
            if (Contains(key))
                return Get(key);
            else
                return def;
        }

        /// <summary>
        /// Returns or sets the value of a key
        /// </summary>
        public dynamic this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        /// <summary>
        /// Clears all the configurations (but not the callbacks)
        /// </summary>
        public void Clear()
        {
            Configs.Clear();
        }

        #endregion

        #region callbacks

        /// <summary>
        /// Whether callbacks should be called now
        /// </summary>
        public enum CallNow
        {
            YES,
            NO,
        };

        /// <summary>
        /// Whether callbacks should be run in main thread
        /// </summary>
        public enum RunOnMainThead
        {
            YES,
            NO,
        };

        /// <summary>
        /// Whether app should wait until callbacks are done
        /// </summary>
        public enum WaitUntilDone
        {
            YES,
            NO,
        };

        /// <summary>
        /// Set a key-value and add a callback
        /// </summary>
        public void SetAndAddCallback(string key, object value, OnChangeHandler callback, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            SetAndAddCallback(key, value, callback, CallNow.NO, runOnMainThread, waitUntilDone);
        }

        /// <summary>
        /// Set a key-value and add a callback
        /// </summary>
        public void SetAndAddCallback(string key, object value, OnChangeHandler callback, CallNow callNow, RunOnMainThead runOnMainThread = RunOnMainThead.NO, WaitUntilDone waitUntilDone = WaitUntilDone.NO)
        {
            Set(key, value);
            AddCallback(key, callback, runOnMainThread, waitUntilDone);
            if (callNow == CallNow.YES)
            {
                Invoke(key);
            }
        }

        /// <summary>
        /// Add a callback to a key
        /// </summary>
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

        /// <summary>
        /// Add a callback to a key
        /// </summary>
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

        /// <summary>
        /// Remove a callback to a key
        /// </summary>
        public void RemoveCallback(string key, OnChangeHandler callback)
        {
            if (Events.ContainsKey(key))
            {
                Events[key] = Delegate.Remove(Events[key], callback);
            }
        }

        /// <summary>
        /// Remove all the callbacks to a key
        /// </summary>
        public void RemoveAllCallback(string key)
        {
            if (Events.ContainsKey(key))
            {
                Events.Remove(key);
            }
        }

        /// <summary>
        /// Remove all the callbacks to all the keys
        /// </summary>
        public void RemoveAllCallback()
        {
            Events.Clear();
        }

        /// <summary>
        /// Try to invoke the callback(s) to a key
        /// </summary>
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

        /// <summary>
        /// Invoke callback(s) to a key
        /// </summary>
        public void Invoke(string key)
        {
            Events[key].DynamicInvoke(Get(key));
        }

        #endregion

        #region utilites

        /// <summary>
        /// Convert to a string, listing all the configurations
        /// </summary>
        override public string ToString()
        {
            return ToString(":");
        }

        /// <summary>
        /// Convert to a string, listing all the configurations
        /// </summary>
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