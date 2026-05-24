import React from 'react';
import AdminOrdersList from './AdminOrdersList.tsx';

/** Отдельный роут для черновиков (?status=Draft на бэкенде через /orders/draft). */
const AdminDraftOrdersList: React.FC = () => <AdminOrdersList draftOnly />;

export default AdminDraftOrdersList;
