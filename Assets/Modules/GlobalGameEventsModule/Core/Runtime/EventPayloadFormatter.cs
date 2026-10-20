using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Game.Events
{
    /// <summary>
    /// Renders the public fields of an event as <c>Name=Value</c> text for the debugger window.
    /// </summary>
    /// <remarks>
    /// The reflection result is cached per type inside this instance. This only ever runs behind the history
    /// service, which is itself only installed when the settings ask for it, so no release build pays for it.
    /// </remarks>
    internal sealed class EventPayloadFormatter
    {
        private readonly Dictionary<Type, FieldInfo[]> _fieldsByType = new Dictionary<Type, FieldInfo[]>();
        private readonly StringBuilder _builder = new StringBuilder();

        internal string Describe(IGameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return string.Empty;
            }

            Type type = gameEvent.GetType();
            if (!_fieldsByType.TryGetValue(type, out FieldInfo[] fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                _fieldsByType.Add(type, fields);
            }

            if (fields.Length == 0)
            {
                return string.Empty;
            }

            _builder.Clear();
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    _builder.Append(", ");
                }

                _builder.Append(fields[i].Name).Append('=');

                object value;
                try
                {
                    value = fields[i].GetValue(gameEvent);
                }
                catch (Exception exception)
                {
                    value = "<" + exception.GetType().Name + ">";
                }

                _builder.Append(value == null ? "null" : value.ToString());
            }

            return _builder.ToString();
        }
    }
}
