using UnityEngine;
using System;
using System.Collections.Generic;

public enum ConfigType
{
    Int,
    Float,
    String,
    Bool
}

public class ConfigItem
{
    public string Name;
    public object Value;
    public ConfigType Type;
    public Action<object> OnValueChanged;

    public ConfigItem(string name, object value, ConfigType type, Action<object> onValueChanged)
    {
        Name = name;
        Value = value;
        Type = type;
        OnValueChanged = onValueChanged;
    }
}

public interface IConfigurable
{
    List<ConfigItem> GetConfigItems();
    string GetConfigTitle();
}

public interface ICanBeHit
{
    void TakeDamage(int damage);
    void OnDeath();
}
