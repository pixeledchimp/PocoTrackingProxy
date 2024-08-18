using PocoTracking.Proxy;

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
            Assert.NotNull(proxy);
            
            Assert.False(trackingActionCalled);

            proxy.Name = "Test";
            Assert.True(trackingActionCalled);

            Assert.Equal("Test", proxy.Name);
            Assert.Equal("Test", poco.Name);
        }
    }
}