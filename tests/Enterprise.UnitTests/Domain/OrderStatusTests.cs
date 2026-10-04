using Enterprise.Domain.Entities;

namespace Enterprise.UnitTests.Domain;

public class OrderStatusTests
{
    [Test]
    public void HistoricalCancelledOrder_CannotReenterTheWorkflow()
    {
        var order = new Order { PreviousStatus = "Cancelled", Status = OrderStatus.New };
        Assert.That(order.IsHistorical, Is.True);
        Assert.Throws<InvalidOperationException>(() => order.ChangeStatus(OrderStatus.Preparing));
        Assert.That(order.Status, Is.EqualTo(OrderStatus.New));
    }
    [Test]
    public void NewOrder_ProgressesThroughPreparingToReady()
    {
        var order = new Order();
        Assert.That(order.Status, Is.EqualTo(OrderStatus.New));
        order.ChangeStatus(OrderStatus.Preparing);
        order.ChangeStatus(OrderStatus.Ready);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Ready));
        Assert.That(Enum.GetValues<OrderStatus>(), Is.EquivalentTo(new[] { OrderStatus.New, OrderStatus.Preparing, OrderStatus.Ready }));
    }

    [TestCase(0, 0)]
    [TestCase(0, 3)]
    [TestCase(2, 0)]
    [TestCase(2, 2)]
    [TestCase(3, 0)]
    [TestCase(3, 2)]
    [TestCase(3, 3)]
    [TestCase(0, 1)]
    [TestCase(2, 4)]
    [TestCase(3, 5)]
    public void InvalidTransitions_DoNotChangeTheOrder(int from, int to)
    {
        var order = new Order { Status = (OrderStatus)from };
        Assert.Throws<InvalidOperationException>(() => order.ChangeStatus((OrderStatus)to));
        Assert.That(order.Status, Is.EqualTo((OrderStatus)from));
    }
}
