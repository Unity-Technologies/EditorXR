using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace ConditionalCompilationUtility
{
	/// <summary>
	/// The Conditional Compilation Utility (CCU) will add defines to the build settings once dependendent classes have been detected. 
	/// In order for this to specified in any project without the project needing to include the CCU, at least one custom attribute 
	/// must be created in the following form:
	/// 
	/// [Conditional(UNITY_CCU)]									// | This is necessary for CCU to pick up the right attributes
	/// public class OptionalDependencyAttribute : Attribute		// | Must derive from System.Attribute
	/// {
	///		public string dependentClass;							// | Required field specifying the fully qualified dependent class
	///		public string define;									// | Required field specifying the define to add
	/// }
	/// 
	/// Then, simply specify the assembly attribute(s) you created:
	/// [assembly: OptionalDependency("UnityEngine.InputNew.InputSystem", "USE_NEW_INPUT")]
	/// [assembly: OptionalDependency("Valve.VR.IVRSystem", "ENABLE_STEAMVR_INPUT")]
	/// 
	/// namespace Foo
	/// { 
	/// ...
	/// }
	/// </summary>
	[InitializeOnLoad]
	public class ConditionalCompilationUtility
	{
		const string kEnableCCU = "UNITY_CCU";

		static ConditionalCompilationUtility()
		{
			var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';').ToList<string>();
			if (!defines.Contains(kEnableCCU, StringComparer.OrdinalIgnoreCase))
			{
				defines.Add(kEnableCCU);
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines.ToArray()));

				// This will trigger another re-compile, which needs to happen, so all the custom attributes will be visible
				return;
			}

			var conditionalAttributeType = typeof(ConditionalAttribute);

			const string kDependentClass = "dependentClass";
			const string kDefine = "define";

			var attributeTypes = GetAssignableTypes(typeof(Attribute), type =>
			{
				var conditionals = (ConditionalAttribute[])type.GetCustomAttributes(conditionalAttributeType, true);

				foreach (var conditional in conditionals)
				{
					if (string.Equals(conditional.ConditionString, kEnableCCU, StringComparison.OrdinalIgnoreCase))
					{
						var dependentClassField = type.GetField(kDependentClass);
						if (dependentClassField == null)
						{
							Debug.LogErrorFormat("[CCU] Attribute type {0} missing field: {1}", type.Name, kDependentClass);
							return false;
						}

						var defineField = type.GetField(kDefine);
						if (defineField == null)
						{
							Debug.LogErrorFormat("[CCU] Attribute type {0} missing field: {1}", type.Name, kDefine);
							return false;
						}

					}
					return true;
				}

				return false;
			});

			var dependencies = new Dictionary<string, string>();
			ForEachAssembly(assembly =>
			{
				var typeAttributes = assembly.GetCustomAttributes(false).Cast<Attribute>();
				foreach (var typeAttribute in typeAttributes)
				{
					if (attributeTypes.Contains(typeAttribute.GetType()))
					{
						var t = typeAttribute.GetType();

						// These fields were already validated in a previous step
						var dependentClass = t.GetField(kDependentClass).GetValue(typeAttribute) as string;
						var define = t.GetField(kDefine).GetValue(typeAttribute) as string;

						if (!string.IsNullOrEmpty(dependentClass) && !string.IsNullOrEmpty(define))
							dependencies.Add(dependentClass, define);
					}
				}
			});

			ForEachAssembly(assembly =>
			{
				foreach (var dependency in dependencies)
				{
					var type = assembly.GetType(dependency.Key);
					if (type != null)
					{
						var define = dependency.Value;
						if (!defines.Contains(define, StringComparer.OrdinalIgnoreCase))
							defines.Add(define);
					}
				}
			});

			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines.ToArray()));
		}

		static void ForEachAssembly(Action<Assembly> callback)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				try
				{
					callback(assembly);
				}
				catch (ReflectionTypeLoadException)
				{
					// Skip any assemblies that don't load properly
					continue;
				}
			}
		}

		static void ForEachType(Action<Type> callback)
		{
			ForEachAssembly(assembly =>
			{
				var types = assembly.GetTypes();
				foreach (var t in types)
					callback(t);
			});
		}

		static IEnumerable<Type> GetAssignableTypes(Type type, Func<Type, bool> predicate = null)
		{
			var list = new List<Type>();
			ForEachType(t =>
			{
				if (type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && (predicate == null || predicate(t)))
					list.Add(t);
			});

			return list;
		}
	}
}
