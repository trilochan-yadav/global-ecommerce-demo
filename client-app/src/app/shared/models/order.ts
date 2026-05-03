export interface Order {
  id: number;
  productId: number;
  quantity: number;
  status: string;
  createdAt: string;
}

export interface CreateOrderRequest {
  productId: number;
  quantity: number;
  unitPrice: number;
  paymentToken: string;
}
