using FluentAssertions;

using PocoTracking.Proxy;

using Proxy;

namespace PocoTrackingProxyFactoryTests
{
    public class PocoTrackingProxyFactory_Tests
    {
        public class Poco
        {
            public virtual string Name { get; set; } = "Default";
        }

        [Fact]
        public void CreateProxyInstance_WithTrackingAction()
        {
            // Arrange
            var poco = new Poco { Name = "Test" };

            var trackingActionCalled = false;

            // Act
            var proxy = poco.CreateProxyInstance((p, propertyName) =>
            {
                trackingActionCalled = true;
            });

            // Assert
            proxy.Should().NotBeNull();
            trackingActionCalled.Should().BeFalse();

            proxy.Name = "Test";

            trackingActionCalled.Should().BeTrue();

            proxy.Name.Should().Be(poco.Name);

            var proxiedPoco = (proxy as IGetProxied<Poco>)!.GetProxiedInstance();
            proxiedPoco.Should().Be(poco);
        }
    }
}