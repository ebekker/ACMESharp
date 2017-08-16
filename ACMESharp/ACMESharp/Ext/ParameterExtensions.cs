using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ACMESharp.Ext
{

	public static class ParameterExtensions
	{
		/// <summary>
		/// Attempts to retrieve a parameter value from the given dictionary as described
		/// by the parameter detail.
		/// </summary>
		/// <remarks>
		/// If the parameter detail specifies a required parameter and the parameter is not
		/// found in the dictionary, will throw a <c>KeyNotFoundException</c>.  If the
		/// parameter is found but not of the target type, an attempt is made to convert it.
		/// Failing that, a <c>InvalidCastException</c> is thrown.  If the parameter is found
		/// and convertable to the target type, the action <c>a</c> will be invoked with its
		/// resolved value.
		/// </remarks>
		public static IReadOnlyDictionary<string, object> GetParameter<T>(
				this IReadOnlyDictionary<string, object> initParams, ParameterDetail p, Action<T> a)
		{
			if (TryGetValue<T>(initParams, p, out var value))
				a(value);
			else if (p.IsRequired)
				throw new KeyNotFoundException($"missing required parameter [{p.Name}]");

			return initParams;
		}

		/// <summary>
		/// Tries to retrieve a value from the parameters dictionary of the specified target type.
		/// </summary>
		/// <returns>
		/// true if able to retrieve a parameter value of the target name and type
		/// </returns>
		/// <remarks>
		/// If a value is found for the given name, but does not match the target type,
		/// then every attempt is made to convert the value to the target type.  Failing
		/// that, an <c>InvalidCastException</c> is thrown.
		/// </remarks>
		public static bool TryGetValue<T>(this IReadOnlyDictionary<string, object> initParams,
				ParameterDetail p, out T value)
		{
			if (initParams.TryGetValue(p.Name, out var val))
			{
				if (val is null)
				{
					value = default(T);
				}
				else if (!(val is T))
				{
					var valType = val.GetType();
					var targType = typeof(T);

					// Make our best effort to convert to or from
					var valTypeConverter = TypeDescriptor.GetConverter(valType);
					if (valTypeConverter.CanConvertTo(targType))
					{
						val = (T)valTypeConverter.ConvertTo(val, targType);
					}
					else
					{
						var targTypeConverter = TypeDescriptor.GetConverter(targType);
						if (targTypeConverter.CanConvertFrom(valType))
						{
							val = (T)targTypeConverter.ConvertFrom(val);
						}
						else
						{
							throw new InvalidCastException($"cannot convert type" +
										$" [{valType.FullName}] to parameter [{p.Name}] type of" +
										$" [{targType.FullName}]");
						}
					}
				}

				value = (T)val;
				return true;
			}

			value = default(T);
			return false;
		}
	}
}
