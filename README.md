# PocoTrackingProxyFactory

Lets say you have a class like this:
```csharp
public class Person
{
	public string Name { get; set; }
	public int Age { get; set; }
}
```

You can create a proxy instance of this class like this:
```csharp
var person = new Person
{
	Name = "John",
	Age = 30,
};

var proxy = PocoTrackingProxyFactory.CreateProxyInstance(person, (p, propertyName) => {

	// This code will be called whenever a property of the proxy object is changed
	Console.WriteLine($"Property {propertyName} changed to {p.GetType().GetProperty(propertyName).GetValue(p)}");

});

proxy.Name = "Jane"; // This will print "Property Name changed to Jane"
proxy.Age = 31; // This will print "Property Age changed to 31"

Console.WriteLine(person.Name); // This will print "Jane"
```

The `CreateProxyInstance` method takes two parameters:
1. The object to create a proxy for
2. A callback that will be called whenever a property of the proxy object is changed

The callback takes two parameters:
1. The proxied object
2. The name of the property that was changed

The proxy object is a wrapper around the given object that allows tracking of the object's properties. When a property of the proxy object is changed, the callback is called with the proxied object and the name of the property that was changed.

The proxy object can be used just like the original object, and changes to the proxy object will be reflected in the original object.

# Remarks
This is a simple implementation of a tracking proxy factory for POCO objects. It can be useful for tracking changes to objects in a non-intrusive way.

CreateProxyInstance is implemented as an extension method for the object class, so it can be called on any object instance.

```csharp

...

var proxy = person.CreateProxyInstance((p, propertyName) => {
	Console.WriteLine($"Property {propertyName} changed to {p.GetType().GetProperty(propertyName).GetValue(p)}");
});

...

```


If the properties of the poco object are not virtual, the proxy object will not be able to track changes to the properties. One way to work around this is to use dynamix instead of var. This way the returned object will not be casted to the original object type and will ise its own properties.

```csharp
dynamic proxy = person.CreateProxyInstance((p, propertyName) => {
	Console.WriteLine($"Property {propertyName} changed to {p.GetType().GetProperty(propertyName).GetValue(p)}");
});

```

Once a Type as been proxied, it will be cached and reused for future calls to CreateProxyInstance. This means that the same proxy Type will be used for the same object type. Nevertheless, the callback will be different for each proxy instance.

# PocoTrackingProxyFactory.CreateProxyType<T>

You can cache the proxy type and create instances of it later. This can be useful if you want to create multiple instances of the same proxy type.

```csharp
var _ = PocoTrackingProxyFactory.CreateProxyType<Person>();
```