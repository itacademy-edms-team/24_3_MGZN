export interface AdminAuthResponse {
  token: string;
  email: string;
  expiresAtUtc: string;
}

export interface AdminMe {
  email: string;
  roles: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminProduct {
  productId: number;
  productName: string;
  productDescription?: string;
  productPrice: number;
  productAvailability: boolean;
  productCategoryId: number;
  productCategoryName?: string;
  productStockQuantity: number;
  reservedQuantity: number;
  imageUrl?: string;
}

export interface AdminOrder {
  orderId: number;
  orderStatus: string;
  rawOrderStatus?: string;
  orderDate: string;
  customerFullname: string;
  customerEmail: string;
  customerPhoneNumber: string;
  orderTotalAmount: number;
  payStatus: string;
  itemsCount: number;
}

export interface CategoryDto {
  categoryId: number;
  categoryName: string;
}

export interface AdminOrderItemDetail {
  orderItemId: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface AdminOrderAuditEntry {
  createdAt: string;
  oldStatus?: string;
  newStatus: string;
  changedBy: string;
}

export interface AdminOrderDetail {
  orderId: number;
  orderStatus: string;
  rawOrderStatus?: string;
  orderDate: string;
  orderTotalAmount: number;
  payStatus: string;
  payMethod: string;
  customerFullname: string;
  customerEmail: string;
  customerPhoneNumber: string;
  sessionId: number;
  shipAddress?: string;
  shipDate?: string;
  shipMethod: string;
  shipCompanyName?: string;
  items: AdminOrderItemDetail[];
  statusHistory: AdminOrderAuditEntry[];
}
