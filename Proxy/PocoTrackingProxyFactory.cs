using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

using Proxy;

namespace PocoTracking.Proxy
{
    public static class PocoTrackingProxyFactory
    {
        private static ConcurrentDictionary<Type, Type> _proxyTypes = new();

        public static T CreateProxyInstance<T>(this T instance, Action<T, string> trackingAction)
            where T : class, new()
        {
            var type = typeof(T);
            var proxyType = PocoTrackingProxyFactory.CreateProxyType<T>();
            var proxyInstance = Activator.CreateInstance(proxyType, instance, trackingAction);
            return (proxyInstance as T)!;
        }

        public static Type CreateProxyType<T>() where T : new()
        {
            var type = typeof(T);

            if (_proxyTypes.TryGetValue(type, out var proxyType))
            {
                return proxyType;
            }

            var typeName = type.Name;
            var trackingProxyAssembly = CreateTrackingProxyAssembly(typeName);
            var moduleBuilder = CreateModule(trackingProxyAssembly, typeName);
            proxyType = CreateProxyType(moduleBuilder, typeName, type);
            _proxyTypes[type] = proxyType;
            return proxyType;
        }

        private static Type CreateProxyType(ModuleBuilder moduleBuilder, string typeName, Type proxyParentClass)
        {
            var proxyTypeBuilder = moduleBuilder.CreateTypeBuilder(typeName, proxyParentClass);

            var instanceField = proxyTypeBuilder.CreatePrivateInstanceField(proxyParentClass);
            var actionField = proxyTypeBuilder.CreatePrivateActionField(proxyParentClass);

            proxyTypeBuilder.CreateGetInstanceMethod(proxyParentClass, instanceField);

            proxyTypeBuilder.CreateConstructor(proxyParentClass, instanceField, actionField);
            proxyTypeBuilder.DefineProxyProperties(proxyParentClass, instanceField, actionField);

            return proxyTypeBuilder.CreateType()!;
        }

        private static void CreateGetInstanceMethod(this TypeBuilder proxyTypeBuilder, Type proxyParentClass, FieldBuilder instanceField)
        {
            proxyTypeBuilder.AddInterfaceImplementation(typeof(IGetProxied<>).MakeGenericType(proxyParentClass));
            var methodBuilder = proxyTypeBuilder.DefineMethod("GetProxiedInstance", MethodAttributes.Public | MethodAttributes.Virtual, proxyParentClass, Type.EmptyTypes);
            var ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, instanceField);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static TypeBuilder CreateTypeBuilder(this ModuleBuilder moduleBuilder, string typeName, Type proxyParentClass)
            => moduleBuilder.DefineType(
                                $"PocoTrackingProxyOf_{typeName}",
                                TypeAttributes.Public | TypeAttributes.Class,
                                proxyParentClass
                            );

        private static void DefineProxyProperties(this TypeBuilder proxyTypeBuilder, Type proxyParentClass, FieldBuilder instanceField, FieldBuilder actionField)
        {
            foreach (var property in proxyParentClass.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyType = property.PropertyType;
                var propertyBuilder = proxyTypeBuilder.DefineProperty(
                    property.Name,
                    PropertyAttributes.HasDefault,
                    propertyType,
                    null
                );

                if (property.CanRead)
                {
                    var getMethodBuilder = proxyTypeBuilder.DefineMethod(
                        $"get_{property.Name}",
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                        propertyType,
                        Type.EmptyTypes
                    );

                    var getILGenerator = getMethodBuilder.GetILGenerator();
                    getILGenerator.Emit(OpCodes.Ldarg_0);
                    getILGenerator.Emit(OpCodes.Ldfld, instanceField);
                    getILGenerator.EmitCall(OpCodes.Callvirt, property.GetGetMethod()!, null);
                    getILGenerator.Emit(OpCodes.Ret);

                    propertyBuilder.SetGetMethod(getMethodBuilder);
                }

                if (property.CanWrite)
                {
                    var setMethodBuilder = proxyTypeBuilder.DefineMethod(
                        $"set_{property.Name}",
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                        null,
                        [propertyType]
                    );

                    var setILGenerator = setMethodBuilder.GetILGenerator();
                    setILGenerator.Emit(OpCodes.Ldarg_0);
                    setILGenerator.Emit(OpCodes.Ldfld, instanceField);
                    setILGenerator.Emit(OpCodes.Ldarg_1);
                    setILGenerator.Emit(OpCodes.Callvirt, property.GetSetMethod()!);
                    setILGenerator.Emit(OpCodes.Ldarg_0);
                    setILGenerator.Emit(OpCodes.Ldfld, actionField);
                    setILGenerator.Emit(OpCodes.Ldarg_0);
                    setILGenerator.Emit(OpCodes.Ldfld, instanceField);
                    setILGenerator.Emit(OpCodes.Ldstr, property.Name);
                    setILGenerator.Emit(OpCodes.Callvirt, typeof(Action<,>).MakeGenericType(proxyParentClass, typeof(string)).GetMethod("Invoke")!);
                    setILGenerator.Emit(OpCodes.Ret);

                    propertyBuilder.SetSetMethod(setMethodBuilder);
                }
            }
        }

        private static void CreateConstructor(this TypeBuilder proxyTypeBuilder, Type type, FieldBuilder instanceField, FieldBuilder actionField)
        {
            var constructorBuilder = proxyTypeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                [type, typeof(Action<,>).MakeGenericType(type, typeof(string))]
            );

            var ilGenerator = constructorBuilder.GetILGenerator();

            CallBaseClassConstructor(type, ilGenerator);
            StoreParametersInFields(type, ilGenerator, instanceField, actionField);

            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void StoreParametersInFields(Type type, ILGenerator ilGenerator, FieldBuilder instanceField, FieldBuilder actionField)
        {
            // Assign instance to instanceField
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, instanceField);

            // Assign action to actionField
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Stfld, actionField);
        }

        private static void CallBaseClassConstructor(Type type, ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, type.GetConstructor(Type.EmptyTypes)!);
        }

        private static FieldBuilder CreatePrivateActionField(this TypeBuilder proxyTypeBuilder, Type type)
        {
            var actionField = proxyTypeBuilder.DefineField(
                            "_action",
                            typeof(Action<,>).MakeGenericType(type, typeof(string)),
                            FieldAttributes.Private
                        );

            return actionField;
        }

        private static FieldBuilder CreatePrivateInstanceField(this TypeBuilder proxyTypeBuilder, Type type)
        {
            var instanceField = proxyTypeBuilder.DefineField(
                            "_instance",
                            type,
                            FieldAttributes.Private
                        );

            return instanceField;
        }

        private static ModuleBuilder CreateModule(AssemblyBuilder trackingProxyAssembly, string typeName)
        {
            return trackingProxyAssembly.DefineDynamicModule($"{typeName}_TrackingProxyModule");
        }

        private static AssemblyBuilder CreateTrackingProxyAssembly(string typeName)
        {
            var assemblyName = new System.Reflection.AssemblyName($"{typeName}_TrackingProxyAssembly");
            var trackingProxyAssembly = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Run
            );
            return trackingProxyAssembly;
        }
    }
}
