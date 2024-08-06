using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using static CheatDeath.CheatDeath;

namespace CheatDeath
{
    internal class CooldownData
    {
        public long worldUID;
        public string globalTime;
        public double worldTime;

        public const string customDataKey = "CheatDeath";

        private double GetCooldownTime()
        {
            if (!ZNet.instance)
                return 0;

            if (cooldownTime.Value == CooldownTime.WorldTime)
                return worldTime == 0 ? 0 : Math.Max(worldTime - ZNet.instance.GetTimeSeconds(), 0);
            else if (DateTime.TryParse(globalTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time))
                return Math.Max((time - GetTime()).TotalSeconds, 0);

            return 0;
        }

        private void SetCooldownTime(double cooldown)
        {
            if (cooldownTime.Value == CooldownTime.GlobalTime)
                globalTime = GetTime().AddSeconds(cooldown).ToString(CultureInfo.InvariantCulture);
            else
                worldTime = ZNet.instance.GetTimeSeconds() + cooldown;
        }

        private static DateTime GetTime()
        {
            return DateTime.Now.ToUniversalTime();
        }

        internal static CooldownData GetCooldownData(List<CooldownData> state, bool createIfEmpty = false)
        {
            long uid = ZNet.instance.GetWorldUID();
            CooldownData data = state.Find(d => d.worldUID == uid);
            if (createIfEmpty && data == null)
            {
                data = new CooldownData
                {
                    worldUID = uid
                };

                state.Add(data);
            }

            return data;
        }

        public static double GetCooldown()
        {
            if (!ZNet.instance)
                return 0;

            CooldownData data = GetCooldownData(GetState());
            return data == null ? 0 : data.GetCooldownTime();
        }

        public static void SetCooldown(double cooldown)
        {
            if (!ZNet.instance)
                return;

            List<CooldownData> state = GetState();

            GetCooldownData(state, createIfEmpty: true).SetCooldownTime(cooldown);

            Player.m_localPlayer.m_customData[customDataKey] = SaveCooldownDataList(state);

            LogInfo($"Cooldown set {TimerString(cooldown)}");
        }

        internal static bool IsOnCooldown()
        {
            return GetCooldown() > 0;
        }

        public static string TimerString(double seconds)
        {
            if (seconds < 60)
                return DateTime.FromBinary(599266080000000000).AddSeconds(seconds).ToString(@"ss\s");

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.Hours > 0)
                return $"{(int)span.TotalHours}{new DateTime(span.Ticks).ToString(@"\h mm\m")}";
            else if (span.Seconds == 0)
                return new DateTime(span.Ticks).ToString(@"mm\m");
            else
                return new DateTime(span.Ticks).ToString(@"mm\m ss\s");
        }

        private static List<CooldownData> GetState()
        {
            return Player.m_localPlayer.m_customData.TryGetValue(customDataKey, out string value) ? GetCooldownDataList(value) : new List<CooldownData>();
        }

        private static List<CooldownData> GetCooldownDataList(string value)
        {
            List<CooldownData> data = new List<CooldownData>();
            SplitToLines(value).Do(line => data.Add(JsonUtility.FromJson<CooldownData>(line)));
            return data;
        }

        private static string SaveCooldownDataList(List<CooldownData> list)
        {
            StringBuilder sb = new StringBuilder();
            list.Do(data => sb.AppendLine(JsonUtility.ToJson(data)));
            return sb.ToString();
        }

        private static IEnumerable<string> SplitToLines(string input)
        {
            if (input == null)
            {
                yield break;
            }

            using (System.IO.StringReader reader = new System.IO.StringReader(input))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }


    }
}
