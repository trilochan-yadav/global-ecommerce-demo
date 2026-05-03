export interface ClickEvent {
  id: number;
  productId: number;
  createdAt: string;
}

export interface Conversion {
  id: number;
  orderId: number;
  customerId: number;
  createdAt: string;
}
