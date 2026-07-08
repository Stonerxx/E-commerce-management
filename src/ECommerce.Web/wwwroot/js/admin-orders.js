(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                orders: [],
                pageIndex: 1,
                pageSize: 10,
                totalCount: 0,
                totalPages: 1,
                filters: {
                    orderNo: '',
                    userId: null,
                    status: '',
                    startTime: '',
                    endTime: ''
                },
                statusCounts: {},
                showShipModal: false,
                shipment: {
                    orderId: null,
                    companyName: '',
                    trackingNo: ''
                }
            };
        },
        computed: {
            visiblePages() {
                const pages = [];
                const maxVisible = 5;
                let start = Math.max(1, this.pageIndex - Math.floor(maxVisible / 2));
                let end = Math.min(this.totalPages, start + maxVisible - 1);
                if (end - start < maxVisible - 1) {
                    start = Math.max(1, end - maxVisible + 1);
                }
                for (let i = start; i <= end; i++) {
                    pages.push(i);
                }
                return pages;
            }
        },
        mounted() {
            this.loadOrders();
        },
        methods: {
            async loadOrders() {
                this.loading = true;
                try {
                    const params = new URLSearchParams({
                        pageIndex: this.pageIndex,
                        pageSize: this.pageSize
                    });

                    if (this.filters.orderNo) params.append('orderNo', this.filters.orderNo);
                    if (this.filters.userId) params.append('userId', this.filters.userId);
                    if (this.filters.status) params.append('status', this.filters.status);
                    if (this.filters.startTime) params.append('startTime', this.filters.startTime);
                    if (this.filters.endTime) params.append('endTime', this.filters.endTime);

                    const response = await fetch(`/api/v1/admin/orders?${params.toString()}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        this.orders = result.data.items || [];
                        this.totalCount = result.data.totalCount || 0;
                        this.totalPages = result.data.totalPages || 1;
                        this.calculateStatusCounts();
                    } else {
                        console.error('加载订单失败:', result.message);
                    }
                } catch (error) {
                    console.error('加载订单异常:', error);
                } finally {
                    this.loading = false;
                }
            },

            calculateStatusCounts() {
                this.statusCounts = {};
                this.orders.forEach(order => {
                    const status = order.status;
                    this.statusCounts[status] = (this.statusCounts[status] || 0) + 1;
                });
            },

            goPage(page) {
                if (page < 1 || page > this.totalPages) return;
                this.pageIndex = page;
                this.loadOrders();
            },

            getStatusText(status) {
                const map = {
                    0: '待支付',
                    1: '已支付',
                    2: '已发货',
                    3: '已完成',
                    4: '已取消'
                };
                return map[status] || '未知';
            },

            getStatusBadge(status) {
                const map = {
                    0: 'text-bg-warning',
                    1: 'text-bg-info',
                    2: 'text-bg-primary',
                    3: 'text-bg-success',
                    4: 'text-bg-secondary'
                };
                return map[status] || 'text-bg-secondary';
            },

            formatDate(dateStr) {
                if (!dateStr) return '-';
                const date = new Date(dateStr);
                return date.toLocaleString('zh-CN');
            },

            openShipModal(orderId) {
                this.shipment.orderId = orderId;
                this.shipment.companyName = '';
                this.shipment.trackingNo = '';
                this.showShipModal = true;
            },

            async submitShipment() {
                if (!this.shipment.companyName || !this.shipment.trackingNo) {
                    alert('请完整填写物流公司和运单号');
                    return;
                }

                try {
                    const response = await fetch(`/api/v1/admin/orders/${this.shipment.orderId}/shipments`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({
                            companyName: this.shipment.companyName,
                            trackingNo: this.shipment.trackingNo,
                            shippedAt: new Date().toISOString()
                        })
                    });
                    const result = await response.json();

                    if (result.success) {
                        this.showShipModal = false;
                        this.loadOrders();
                    } else {
                        alert(result.message || '发货失败');
                    }
                } catch (error) {
                    console.error('发货异常:', error);
                    alert('发货失败，请稍后重试');
                }
            },

            async adminCancelOrder(orderId) {
                if (!confirm('确定要强制取消这个订单吗？此操作不可恢复！')) return;

                try {
                    const response = await fetch(`/api/v1/admin/orders/${orderId}/cancel`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({ reason: '后台强制取消' })
                    });
                    const result = await response.json();

                    if (result.success) {
                        this.loadOrders();
                    } else {
                        alert(result.message || '取消订单失败');
                    }
                } catch (error) {
                    console.error('取消订单异常:', error);
                    alert('取消订单失败，请稍后重试');
                }
            },

            shipOrder(orderId) {
                this.openShipModal(orderId);
            }
        }
    }).mount('#adminOrdersApp');
})();
