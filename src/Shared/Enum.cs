namespace Shared;

public enum OrderStatus { Pending, PaymentProcessed, PaymentFailed, Shipped, Failed }
public enum PaymentStatus { Pending, Completed, Failed }
public enum ShippingStatus { Created, InTransit, Delivered }
public enum StockAction { Reduce, Restore }