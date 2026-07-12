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
                    status: '',
                    startTime: '',
                    endTime: ''
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

                    if (this.filters.status) params.append('status', this.filters.status);
                    if (this.filters.startTime) params.append('startTime', this.filters.startTime);
                    if (this.filters.endTime) params.append('endTime', this.filters.endTime);

                    const response = await fetch(`/api/v1/orders?${params.toString()}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        this.orders = result.data.items || [];
                        this.totalCount = result.data.totalCount || 0;
                        this.totalPages = result.data.totalPages || 1;
                    } else {
                        console.error('加载订单失败:', result.message);
                    }
                } catch (error) {
                    console.error('加载订单异常:', error);
                } finally {
                    this.loading = false;
                }
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

            async cancelOrder(orderId) {
                if (!confirm('确定要取消这个订单吗？')) return;

                try {
                    const response = await fetch(`/api/v1/orders/${orderId}/cancel`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({ reason: '用户主动取消' })
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

            async confirmOrder(orderId) {
                if (!confirm('确认已收到商品？')) return;

                try {
                    const response = await fetch(`/api/v1/orders/${orderId}/confirm`, {
                        method: 'POST',
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success) {
                        this.loadOrders();
                    } else {
                        alert(result.message || '确认收货失败');
                    }
                } catch (error) {
                    console.error('确认收货异常:', error);
                    alert('确认收货失败，请稍后重试');
                }
            },

            goPay(orderId) {
                // TEMP_DEMO_PAYMENT: member5 合入前先跳转到临时模拟支付页。
                window.location.href = `/payment/${orderId}`;
            }
        }
    }).mount('#ordersApp');
})();
